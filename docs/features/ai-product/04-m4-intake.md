# M4｜抽牌前澄清對話（intake）

> 目標：抽牌前用一段輕量 AI 對話把「真正想問的問題」與情境談清楚，產出精煉問題＋情境摘要＋建議牌陣，帶進抽牌流程並存進 `readings.context`，讓 M1 解讀更貼合。
> 免費、匿名可用、用便宜模型（`AI_MODEL_LIGHT`）。前置：M1 完成（`CompleteJsonAsync` 與 007 的 `context` 欄位都已存在）。

## 1. Migration
無（007 已加 `readings.context`）。

## 2. 後端

### 2.1 Rate limit policy `"intake"`
`Program.cs` 照既有 `"anonymous-draw"` policy 的寫法（同一段 `AddRateLimiter` 裡）再加一個：**每 IP 10 req/min**，拒絕回 429。

### 2.2 新檔 `Services/IntakeService.cs`

```csharp
using System.Text.Json;
using TarotApi.Services.Ai;

namespace TarotApi.Services;

public record IntakeResult(
    string Reply, bool Ready,
    string? RefinedQuestion, string? ContextSummary, string? RecommendedSpread);

public class IntakeService(IAiClient ai, AiModelOptions models)
{
    public const int MaxMessages = 12;
    public const int MaxMessageLength = 500;

    private const string SystemPrompt = """
        你是塔羅占卜的引導者。你的目標不是解牌，而是幫使用者把「想問的問題」與「所處情境」談清楚。
        規則：
        - 一次最多問 1 個關鍵澄清問題；語氣溫和簡短（50 字內）。
        - 問題已經夠清楚就不要硬問，直接 ready=true。通常 1~3 輪內收斂。
        - ready=true 時必須同時給出：refinedQuestion（一句精煉問題）、contextSummary（1~3 句情境摘要）、recommendedSpread（從下列挑選）。
        可用牌陣與適用情境:
        - "single": 單張指引,適合簡單明確的小問題
        - "three-card-time": 過去/現在/未來,適合想了解事情演變
        - "three-card-problem": 問題/原因/解法,適合卡關想找出路
        - "three-card-linear": 三張自由解,通用
        - "celtic-cross": 十張深度解析,適合重大複雜的課題
        使用繁體中文（台灣用語）。
        """;

    // JSON Schema（結構化輸出，保證回傳可解析）
    private static readonly Dictionary<string, JsonElement> Schema = BuildSchema();
    private static Dictionary<string, JsonElement> BuildSchema()
    {
        const string json = """
        {
          "type": "object",
          "properties": {
            "reply": { "type": "string" },
            "ready": { "type": "boolean" },
            "refinedQuestion": { "type": ["string","null"] },
            "contextSummary": { "type": ["string","null"] },
            "recommendedSpread": { "type": ["string","null"],
              "enum": ["single","three-card-time","three-card-problem","three-card-linear","celtic-cross", null] }
          },
          "required": ["reply","ready","refinedQuestion","contextSummary","recommendedSpread"],
          "additionalProperties": false
        }
        """;
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
    }

    public async Task<IntakeResult> RunAsync(IReadOnlyList<AiMessage> messages, CancellationToken ct)
    {
        var request = new AiRequest(models.Light, SystemPrompt, messages, MaxTokens: 600);
        return await ai.CompleteJsonAsync<IntakeResult>(request, Schema, ct);
    }
}
```

註冊：`builder.Services.AddScoped<IntakeService>();`

### 2.3 新檔 `Controllers/IntakeController.cs`

```csharp
[ApiController]
[Route("api/intake")]
public class IntakeController(IntakeService intake) : ControllerBase
{
    public record IntakeRequestDto(List<AiMessage> Messages);

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("intake")]
    public async Task<IActionResult> Post([FromBody] IntakeRequestDto dto, CancellationToken ct)
    {
        if (dto.Messages is null or { Count: 0 } || dto.Messages.Count > IntakeService.MaxMessages)
            return BadRequest(new { code = "INVALID_MESSAGES" });
        if (dto.Messages.Any(m => string.IsNullOrWhiteSpace(m.Content)
                || m.Content.Length > IntakeService.MaxMessageLength
                || (m.Role != "user" && m.Role != "assistant")))
            return BadRequest(new { code = "INVALID_MESSAGES" });
        if (dto.Messages[^1].Role != "user")
            return BadRequest(new { code = "INVALID_MESSAGES" });

        return Ok(await intake.RunAsync(dto.Messages, ct));
    }
}
```

對話歷史由**前端持有**、每輪整段送來，後端無狀態（與匿名抽牌同哲學）。

### 2.4 `context` 寫進 reading
- `ReadingCreateDto` 與 `ReadingImportDto` 各加選填欄位 `Context`（型別 `JsonDocument?` 或 `Dictionary<string,string>?`，跟現有 DTO json 綁定方式一致即可）。
- `ReadingService.CreateReading` / `ImportReading` 把它寫進 `reading.Context`。
- 內容形狀固定：`{ "refinedQuestion": "...", "contextSummary": "..." }`。
- M1 的 `BuildInterpretationInput` 已會讀 `reading.Context`——確認串起來即可。

## 3. 前端

### 3.1 Proxy `src/routes/api/intake/+server.ts`
POST JSON 轉發（無需 session；有 session 也不用帶）。照 M2 claim-daily proxy 的普通 JSON 模式。

### 3.2 新元件 `src/lib/components/IntakeChat.svelte`

- 入口：首頁 `QuestionInput` 旁加一顆「幫我把問題想清楚 ✨」按鈕，點了展開此元件（可收合）。
- 內部狀態（Svelte 5 runes）：
  ```typescript
  let messages = $state<{ role: 'user' | 'assistant'; content: string }[]>([]);
  let input = $state('');
  let loading = $state(false);
  let result = $state<IntakeResult | null>(null);   // ready=true 時整包留下
  ```
- 送出：把 `{role:'user', content: input}` push 進 `messages` → POST `/api/intake` 帶全部 messages → 把 `reply` 以 assistant push 回列表；`ready=true` 時顯示摘要卡片（精煉問題／情境摘要／建議牌陣）＋「就這樣抽牌」按鈕。
- 「就這樣抽牌」→ 呼叫 props 的 `onready(result)`。
- 429 → 顯示「稍後再試」；超過 12 輪由前端擋（第 6 輪起提示「差不多了，直接抽牌吧」）。

### 3.3 首頁接線（`routes/+page.svelte` ＋ `+page.server.ts`）
- `onready`：把 `refinedQuestion` 填入 QuestionInput、`recommendedSpread` 設為 SpreadSelector 選取值（值域即前端 SpreadType key，直接用）、`contextSummary` 存進頁面狀態。
- draw form 加 hidden input `context`（JSON 字串 `{refinedQuestion, contextSummary}`）。
- `+page.server.ts` 的 `actions.draw`：登入路徑把 `context` 解析後帶進 `POST /api/readings` body；匿名路徑把它塞進 localStorage 的 pending payload（跟 clientToken 同一包），`importPending` 時帶進 `POST /api/readings/import`。

## 4. 驗證
- `dotnet build`、`pnpm check` 過。
- curl（免 token）：
  ```bash
  curl -s -X POST http://localhost:5098/api/intake -H "Content-Type: application/json" \
    -d '{"messages":[{"role":"user","content":"我最近好煩"}]}'
  # 預期：ready=false，reply 是一個澄清問題
  ```
  連續補充 2~3 輪後 → `ready=true` 且三個產物齊全、`recommendedSpread` 在合法值域內。
- 瀏覽器：intake → 抽牌 → AI 解讀內容明顯引用了情境摘要；DB `readings.context` 有值。
- 匿名：intake → 抽牌 → 登入 → import → 該筆 reading 的 `context` 有值。
- 快速連打 11 次 → 429。

## 5. 禁區
- intake 不扣點、不留存對話到 DB。
- 不強制使用者走 intake（永遠可以直接抽）。
- 不建 `readers` 表、不動牌陣邏輯。

## 6. 常見坑
- 結構化輸出已由 schema 保證合法 JSON——**不要**再對回傳字串做 regex 清洗。
- `recommendedSpread` 的值域就是前端 spread key，全鏈路（intake→SpreadSelector→draw API）不需要任何轉換；不要把 `weekly-fortune`/`custom` 加進 enum。
- 匿名 pending payload 已有既定形狀（localStorage），加 `context` 欄位時看清楚現有讀寫兩端（`+page.svelte` 與 `SavePendingReadingDialog`）一起改。

## 7. Definition of Done
- [ ] intake 對匿名/登入皆可用、限流生效
- [ ] ready 產物一鍵帶入抽牌；`readings.context` 落地（含匿名 import 路徑）
- [ ] 解讀 prompt 實際吃到 context（後端 log 或直接看解讀內容驗證）
- [ ] `.env.example` 無新增（沿用 M1 的 AI_MODEL_LIGHT）；`CLAUDE.md` API 表加 `POST /api/intake`
- [ ] 跑過 `/qa`
