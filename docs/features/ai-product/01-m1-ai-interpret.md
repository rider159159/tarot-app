# M1｜AI 解讀（IAiClient ＋ SSE 串流）

> 目標：抽完牌按「AI 解讀」→ 後端呼叫 Claude → 逐字串流顯示 → 存進 `readings.interpretation`。
> **本階段免費不扣點**（M2 才接點數）。匿名者不可解讀，導去登入。
> 先讀 `00-overview.md` §4 全域慣例。

## 0. 前置條件
- `.env` 已有 `ANTHROPIC_API_KEY`（使用者提供；沒有就停下來要）。
- `.env.example` 補上 M1 區塊四個變數（見 overview §4.6）。

## 1. Migration `007_reading_ai_fields.sql`

```sql
-- 007_reading_ai_fields.sql
-- AI 解讀相關欄位：解讀者、intake 情境、解讀時間
ALTER TABLE readings
  ADD COLUMN IF NOT EXISTS reader_id      text,
  ADD COLUMN IF NOT EXISTS context        jsonb,
  ADD COLUMN IF NOT EXISTS interpreted_at timestamptz;

COMMENT ON COLUMN readings.context IS 'intake 產物：{refinedQuestion, contextSummary}（M4 開始寫入）';
```

套用（Supabase MCP `apply_migration`，name=`007_reading_ai_fields`）→ `list_tables` 確認三個欄位存在，才准動後端。

## 2. 後端

### 2.1 安裝官方 SDK
```bash
cd backend/TarotApi && dotnet add package Anthropic
```

### 2.2 新檔 `Services/Ai/IAiClient.cs`

```csharp
using System.Text.Json;

namespace TarotApi.Services.Ai;

public record AiMessage(string Role, string Content);   // "user" | "assistant"

public record AiRequest(
    string Model,
    string? System,
    IReadOnlyList<AiMessage> Messages,
    int MaxTokens = 1500);
// 注意：刻意沒有 Temperature——不要加（新一代 Claude 模型不收 sampling 參數）。

public interface IAiClient
{
    IAsyncEnumerable<string> StreamAsync(AiRequest request, CancellationToken ct = default);
    Task<string> CompleteAsync(AiRequest request, CancellationToken ct = default);
    /// <summary>結構化輸出（M4 intake 用）。schema 為 JSON Schema object。</summary>
    Task<T> CompleteJsonAsync<T>(AiRequest request, Dictionary<string, JsonElement> schema, CancellationToken ct = default);
}
```

### 2.3 新檔 `Services/Ai/ClaudeAiClient.cs`

用官方 SDK（`using Anthropic; using Anthropic.Models.Messages;`），**不要**自己用 HttpClient 打 api.anthropic.com。

```csharp
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace TarotApi.Services.Ai;

public class ClaudeAiClient : IAiClient
{
    private readonly AnthropicClient _client;

    public ClaudeAiClient()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY is required");
        _client = new AnthropicClient { ApiKey = apiKey };
    }

    private static MessageCreateParams Build(AiRequest r) => new()
    {
        Model = r.Model,
        MaxTokens = r.MaxTokens,
        System = r.System,
        Messages = r.Messages.Select(m => new MessageParam
        {
            Role = m.Role == "assistant" ? Role.Assistant : Role.User,
            Content = m.Content
        }).ToList()
    };

    public async IAsyncEnumerable<string> StreamAsync(
        AiRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var ev in _client.Messages.CreateStreaming(Build(request)).WithCancellation(ct))
        {
            if (ev.TryPickContentBlockDelta(out var delta) &&
                delta.Delta.TryPickText(out var text))
            {
                yield return text.Text;
            }
        }
    }

    public async Task<string> CompleteAsync(AiRequest request, CancellationToken ct = default)
    {
        var response = await _client.Messages.Create(Build(request));
        var sb = new StringBuilder();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? tb)) sb.Append(tb.Text);
        }
        return sb.ToString();
    }

    public async Task<T> CompleteJsonAsync<T>(
        AiRequest request, Dictionary<string, JsonElement> schema, CancellationToken ct = default)
    {
        var p = Build(request);
        p.OutputConfig = new OutputConfig
        {
            Format = new JsonOutputFormat { Schema = schema }
        };
        var response = await _client.Messages.Create(p);
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out TextBlock? tb))
                return JsonSerializer.Deserialize<T>(tb.Text, JsonOpts)!;
        }
        throw new InvalidOperationException("AI response contained no text block");
    }

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };
}
```

> 編譯若對個別型別報錯（例如 `System` / `Model` 需要明確轉型、`OutputConfig` 是 init-only），依編譯器訊息微調——SDK 型別名以編譯器為準，不要改成手刻 HTTP。

### 2.4 新檔 `Services/Ai/AiModelOptions.cs`

```csharp
namespace TarotApi.Services.Ai;

public sealed class AiModelOptions
{
    public string Interpret { get; } =
        Environment.GetEnvironmentVariable("AI_MODEL_INTERPRET") ?? "claude-sonnet-4-6";
    public string Light { get; } =
        Environment.GetEnvironmentVariable("AI_MODEL_LIGHT") ?? "claude-haiku-4-5";
}
```

### 2.5 `Program.cs` 註冊（放在現有 `AddScoped<PromptBuilder>` 附近）

```csharp
builder.Services.AddSingleton<AiModelOptions>();
builder.Services.AddSingleton<IAiClient, ClaudeAiClient>();
```

### 2.6 `PromptBuilder.cs` 加解讀輸入組裝

先打開 `Services/PromptBuilder.cs` 看 `BuildReadingDto(Reading)` 回傳的 `ExportReadingDto` 實際欄位名，再實作（**重用它**，不要重新組牌卡資料）：

```csharp
private const string InterpretSystemPrompt = """
    你是一位資深塔羅諮商者，風格溫柔而務實。依抽牌結果，針對使用者的問題與情境提供深入解讀：
    1) 逐張說明牌在其位置的意義；
    2) 牌與牌之間的關聯與整體敘事；
    3) 對應到使用者的具體處境；
    4) 最後給 2-3 個可執行的反思或行動建議。
    使用繁體中文（台灣用語）。語氣溫暖但不過度肯定、不下命定式斷言、不做醫療/法律/生死預測。
    直接輸出解讀內容，不要開場白、不要 markdown 標題；用自然段落，適量條列即可。
    """;

/// <summary>組出 AI 解讀的 (system, user) 輸入。memorySummary 於 M5 開始傳入。</summary>
public (string System, string User) BuildInterpretationInput(Reading reading, string? memorySummary = null)
{
    var dto = BuildReadingDto(reading);   // 既有方法：牌名/正逆/位置/關鍵字/牌義都在裡面
    var sb = new StringBuilder();

    sb.AppendLine($"【牌陣】{dto.SpreadType.Label}");
    if (!string.IsNullOrWhiteSpace(dto.Question))
        sb.AppendLine($"【使用者的問題】{dto.Question}");

    // M4 之後 reading.Context 會有 intake 產物
    if (reading.Context is not null)
        sb.AppendLine($"【使用者補充的情境】{reading.Context.RootElement.GetRawText()}");

    if (!string.IsNullOrWhiteSpace(memorySummary))
        sb.AppendLine($"【你已知的這位使用者背景（供參考，不必逐條回應）】{memorySummary}");

    sb.AppendLine("【抽到的牌】");
    foreach (var c in dto.Cards)
    {
        sb.AppendLine($"- 位置「{c.Position}」：{c.Name}（{(c.Orientation == "reversed" ? "逆位" : "正位")}）");
        sb.AppendLine($"  關鍵字：{string.Join("、", c.Keywords)}；牌義：{c.Meaning}");
    }
    sb.AppendLine("請開始解讀。");
    return (InterpretSystemPrompt, sb.ToString());
}
```

（欄位名 `Position/Name/Orientation/Keywords/Meaning` 以檔案內實際 DTO 為準，照實調整。）

### 2.7 `ReadingService.cs` 加存檔方法

```csharp
public async Task SaveInterpretationAsync(Guid userId, Guid readingId, string interpretation, string readerId)
{
    var reading = await _context.Readings
        .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId && r.DeletedAt == null);
    if (reading is null) return;
    reading.Interpretation = interpretation;
    reading.ReaderId = readerId;
    reading.InterpretedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
}
```

`Models/Reading.cs` 加三個屬性 ＋ `TarotDbContext` 對映（照 `client_token` 既有寫法對 snake_case 欄位）：

```csharp
public string? ReaderId { get; set; }          // reader_id
public JsonDocument? Context { get; set; }     // context (jsonb)
public DateTime? InterpretedAt { get; set; }   // interpreted_at
```

### 2.8 `ReadingController.cs` 加 interpret endpoint

```csharp
private const string DefaultReaderId = "default-warm";

[HttpPost("{id:guid}/interpret")]
public async Task<IActionResult> Interpret(Guid id, CancellationToken ct)
{
    var userId = User.GetUserId();
    var reading = await _readingService.GetRawReadingById(userId, id);
    if (reading is null) return NotFound();

    // 冪等：已有解讀 → 直接回存檔，不再呼叫 AI（M2 起也不扣點）
    if (!string.IsNullOrEmpty(reading.Interpretation))
    {
        StartSse();
        await WriteSseAsync(new { delta = reading.Interpretation }, ct);
        await WriteSseAsync(new { done = true }, ct);
        return new EmptyResult();
    }

    // [M2 插入點 A：扣點，失敗回 402]

    StartSse();
    var sb = new StringBuilder();
    try
    {
        var (system, user) = _promptBuilder.BuildInterpretationInput(reading);
        var request = new AiRequest(_aiModels.Interpret, system,
            [new AiMessage("user", user)], MaxTokens: 2000);

        await foreach (var delta in _ai.StreamAsync(request, ct))
        {
            sb.Append(delta);
            await WriteSseAsync(new { delta }, ct);
        }
        await _readingService.SaveInterpretationAsync(userId, id, sb.ToString(), DefaultReaderId);
        await WriteSseAsync(new { done = true }, ct);
    }
    catch (OperationCanceledException)
    {
        // 使用者中途離開：保留已生成的部分，下次進來直接看到
        if (sb.Length > 0)
            await _readingService.SaveInterpretationAsync(userId, id, sb.ToString(), DefaultReaderId);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AI interpret failed for reading {ReadingId}", id);
        // [M2 插入點 B：退點]
        await WriteSseAsync(new { error = "AI 服務暫時無法使用，請稍後再試", code = "AI_UNAVAILABLE" }, CancellationToken.None);
    }
    return new EmptyResult();
}

private void StartSse()
{
    Response.Headers.ContentType = "text/event-stream; charset=utf-8";
    Response.Headers.CacheControl = "no-cache";
    Response.Headers["X-Accel-Buffering"] = "no";
}

private async Task WriteSseAsync(object payload, CancellationToken ct)
{
    await Response.WriteAsync($"data: {JsonSerializer.Serialize(payload)}\n\n", ct);
    await Response.Body.FlushAsync(ct);
}
```

建構子注入 `IAiClient _ai`、`AiModelOptions _aiModels`、`ILogger<ReadingController> _logger`（`PromptBuilder` 與 `ReadingService` 已注入就沿用）。

## 3. 前端

### 3.1 新檔 `src/lib/utils/sse.ts`

```typescript
export type SseEvent = { delta?: string; done?: boolean; error?: string; code?: string };

/** 讀取 SSE 回應，逐 event 呼叫 onEvent。resolve 表示串流結束。 */
export async function readSse(response: Response, onEvent: (ev: SseEvent) => void): Promise<void> {
	const reader = response.body!.getReader();
	const decoder = new TextDecoder();
	let buffer = '';
	for (;;) {
		const { done, value } = await reader.read();
		if (done) break;
		buffer += decoder.decode(value, { stream: true });
		const chunks = buffer.split('\n\n');
		buffer = chunks.pop() ?? '';
		for (const chunk of chunks) {
			const line = chunk.split('\n').find((l) => l.startsWith('data: '));
			if (line) onEvent(JSON.parse(line.slice(6)));
		}
	}
}
```

### 3.2 新檔 `src/routes/api/readings/[id]/interpret/+server.ts`（SSE proxy）

先看 `src/lib/server/api.ts` 怎麼取得後端 base URL（`INTERNAL_API_URL`），**沿用同一套 env 讀法**：

```typescript
import type { RequestHandler } from './$types';
// base URL 取得方式照 lib/server/api.ts

export const POST: RequestHandler = async ({ params, locals, fetch }) => {
	const { session } = await locals.safeGetSession();
	if (!session) {
		return new Response(JSON.stringify({ code: 'UNAUTHORIZED' }), { status: 401 });
	}
	const upstream = await fetch(`${API_BASE}/api/readings/${params.id}/interpret`, {
		method: 'POST',
		headers: { Authorization: `Bearer ${session.access_token}` }
	});
	if (!upstream.ok && upstream.headers.get('content-type')?.includes('json')) {
		return new Response(await upstream.text(), { status: upstream.status });
	}
	return new Response(upstream.body, {
		status: upstream.status,
		headers: {
			'Content-Type': 'text/event-stream; charset=utf-8',
			'Cache-Control': 'no-cache',
			'X-Accel-Buffering': 'no'
		}
	});
};
```

⚠️ 檢查 `src/hooks.server.ts` 的路由守衛：若 `/api/` 開頭的請求會被 redirect 到 `/login`，改成「`/api/` 前綴不 redirect、由 endpoint 自己回 401」（fetch 吃到 redirect 會壞）。

### 3.3 `ReadingDisplay.svelte` 加解讀區塊

確認前端 `Reading` 型別有 `id` 與 `interpretation`（`lib/types/index.ts`，沒有就補——後端 DTO 本來就有回）。核心邏輯：

```svelte
<script lang="ts">
	import { readSse } from '$lib/utils/sse';
	// 既有 props 之外，需要 reading.id / reading.interpretation / 是否已登入(isLoggedIn)
	let interpretation = $state(reading?.interpretation ?? '');
	let interpreting = $state(false);
	let interpretError = $state('');

	async function interpret() {
		interpreting = true;
		interpretError = '';
		try {
			const res = await fetch(`/api/readings/${reading.id}/interpret`, { method: 'POST' });
			if (!res.ok) {
				const body = await res.json().catch(() => ({}));
				interpretError = body.code === 'INSUFFICIENT_CREDITS' ? '點數不足' : '解讀失敗，請稍後再試';
				return;
			}
			await readSse(res, (ev) => {
				if (ev.delta) interpretation += ev.delta;
				if (ev.error) interpretError = ev.error;
			});
		} finally {
			interpreting = false;
		}
	}
</script>
```

UI 規則：
- `interpretation` 非空 → 顯示解讀文字（打字機效果 = 串流時逐字 append，本身就有）。
- 空 ＋ 已登入 ＋ 有 `reading.id` → 「✨ AI 解讀」按鈕（`interpreting` 時 disabled ＋ loading 樣式，防連點）。
- 匿名（無 `reading.id`）→ 顯示「登入後可使用 AI 解讀」，連到現有登入/儲存流程（`SavePendingReadingDialog` / `AnonymousCta` 那條路）。
- `interpretError` 非空 → 錯誤提示 ＋ 重試按鈕。

## 4. 驗證

```bash
cd backend/TarotApi && dotnet build          # 0 error
cd frontend && pnpm check                    # 0 error
docker compose up --build                    # 起本地環境
```

用 CLAUDE.md「測試帳號」段落換 `TOKEN` 後：

```bash
# 先建一筆 reading 拿 id（或用既有的），然後：
curl -N -X POST -H "Authorization: Bearer $TOKEN" \
  http://localhost:5098/api/readings/<id>/interpret
# 預期：data: {"delta":"..."} 逐塊出現（不是一次全吐），最後 data: {"done":true}
# 再打一次同一 id：一個大 delta（存檔內容）+ done，且後端 log 沒有再呼叫 AI
```

瀏覽器（http://localhost:5173）：登入 → 抽牌 → 按 AI 解讀 → 逐字出現 → 重新整理後解讀仍在（已存檔）。

## 5. 禁區（M1 不做）
- 不做扣點/點數（M2）、不做追問（M3）、不做 intake（M4）、不做記憶（M5）。
- 不寫 `OpenAiAiClient`（介面留著就好）。
- 不做 persona 選擇 UI（寫死 `default-warm`）。
- 不動匿名抽牌、import、weekly-fortune 任何既有流程。
- 不改 `oci-infra`（除非部署後串流驗收失敗，見 overview §3）。

## 6. 常見坑
- **migration 沒套用就跑後端** → `context` 欄位不存在，EF 噴錯。先套用先驗證。
- SSE 每寫一個 event 必須 `FlushAsync`，漏了會整批吐。
- 本地 Vite dev server 對 proxy 串流沒問題；若見「卡住最後全吐」先確認後端 Flush，再看反代。
- `JsonDocument`（`Context` 欄位）是 IDisposable，由 EF 管生命週期，不要手動 Dispose。
- 錯誤發生在**開始串流之後**只能用 SSE `{"error":...}` 傳遞（HTTP status 已送出改不了）。

## 7. Definition of Done
- [ ] 007 已套用線上 DB（`list_migrations` 可見）
- [ ] `dotnet build`、`pnpm check` 皆 0 error
- [ ] 登入者：解讀逐字串流、重整後仍在、重按不重新生成
- [ ] 匿名者：看到登入引導，打 API 直接 401
- [ ] AI 失敗時前端顯示可重試的錯誤（拔掉 API key 測一次）
- [ ] `.env.example` 已補 M1 變數；`CLAUDE.md` API 表加 `POST /api/readings/{id}/interpret`
- [ ] 跑過 `/qa`
