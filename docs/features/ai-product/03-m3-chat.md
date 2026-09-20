# M3｜解讀後追問對話（多輪、SSE、扣點）

> 目標：解讀完成後，使用者可就這次牌陣多輪追問；每則使用者訊息扣 `CHAT_COST`，訊息留存。
> 前置：M1、M2 完成。SSE 格式、proxy、readSse 全部沿用 M1 的既有件，不要重寫。

## 1. Migration `009_reading_messages.sql`

```sql
-- 009_reading_messages.sql
CREATE TABLE reading_messages (
  id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  reading_id uuid NOT NULL REFERENCES readings(id) ON DELETE CASCADE,
  user_id    uuid NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  role       text NOT NULL CHECK (role IN ('user','assistant')),
  content    text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_reading_messages ON reading_messages (reading_id, created_at);
ALTER TABLE reading_messages ENABLE ROW LEVEL SECURITY;
```

套用並驗證後才動後端。

## 2. 後端

### 2.1 Entity `Models/ReadingMessage.cs` ＋ DbContext `DbSet<ReadingMessage> ReadingMessages`（snake_case 對映照舊）。

### 2.2 新檔 `Services/ChatService.cs`

```csharp
public class ChatService(TarotDbContext db, PromptBuilder promptBuilder, AiModelOptions models)
{
    public const int MaxHistoryMessages = 20;   // 只帶最近 20 則進 context 控 token
    public const int MaxContentLength = 1000;   // 單則使用者訊息長度上限

    public async Task<List<ReadingMessage>> GetMessagesAsync(Guid userId, Guid readingId) =>
        await db.ReadingMessages
            .Where(m => m.ReadingId == readingId && m.UserId == userId)
            .OrderBy(m => m.CreatedAt).ToListAsync();

    public async Task AddMessageAsync(Guid userId, Guid readingId, string role, string content)
    {
        db.ReadingMessages.Add(new ReadingMessage {
            ReadingId = readingId, UserId = userId, Role = role, Content = content });
        await db.SaveChangesAsync();
    }

    /// <summary>組追問請求：persona + 牌陣脈絡 + 既有解讀 + 近期對話 + 新問題。</summary>
    public AiRequest BuildChatRequest(Reading reading, List<ReadingMessage> history,
        string newUserMessage, string? memorySummary = null)
    {
        var (interpretSystem, readingContext) = promptBuilder.BuildInterpretationInput(reading, memorySummary);
        var system = interpretSystem + """

            現在是解讀後的追問階段。使用者會就這次牌陣繼續提問。
            回應規則：聚焦在使用者的追問、引用已抽出的牌與既有解讀；每次回應 100~300 字；
            不要重新完整解牌，除非使用者要求。
            """;
        var messages = new List<AiMessage>
        {
            new("user", readingContext + "\n\n【本次的完整解讀】\n" + reading.Interpretation)
        };
        foreach (var m in history.TakeLast(MaxHistoryMessages))
            messages.Add(new AiMessage(m.Role, m.Content));
        messages.Add(new AiMessage("user", newUserMessage));
        return new AiRequest(models.Interpret, system, messages, MaxTokens: 1000);
    }
}
```

註冊：`builder.Services.AddScoped<ChatService>();`

### 2.3 `ReadingController` 加兩個 endpoint

```csharp
public record ChatRequestDto(string Content, string ClientMessageId);

[HttpGet("{id:guid}/messages")]
public async Task<IActionResult> GetMessages(Guid id)
{
    var userId = User.GetUserId();
    if (await _readingService.GetRawReadingById(userId, id) is null) return NotFound();
    var msgs = await _chatService.GetMessagesAsync(userId, id);
    return Ok(msgs.Select(m => new { m.Id, m.Role, m.Content, m.CreatedAt }));
}

[HttpPost("{id:guid}/chat")]
public async Task<IActionResult> Chat(Guid id, [FromBody] ChatRequestDto dto, CancellationToken ct)
{
    var userId = User.GetUserId();
    if (string.IsNullOrWhiteSpace(dto.Content) || dto.Content.Length > ChatService.MaxContentLength
        || string.IsNullOrWhiteSpace(dto.ClientMessageId) || dto.ClientMessageId.Length > 64)
        return BadRequest(new { code = "INVALID_MESSAGE" });

    var reading = await _readingService.GetRawReadingById(userId, id);
    if (reading is null) return NotFound();
    if (string.IsNullOrEmpty(reading.Interpretation))
        return BadRequest(new { code = "INTERPRET_FIRST", message = "請先進行 AI 解讀" });

    var idemKey = $"chat:{id}:{dto.ClientMessageId}";
    var charge = await _credits.ChargeAsync(userId, CreditService.ChatCost, "chat", idemKey,
        refType: "reading", refId: id.ToString());
    if (charge == ChargeResult.InsufficientCredits)
        return StatusCode(402, new { code = "INSUFFICIENT_CREDITS", message = "點數不足" });
    if (charge == ChargeResult.AlreadyProcessed)
        return Conflict(new { code = "DUPLICATE_MESSAGE" });

    var history = await _chatService.GetMessagesAsync(userId, id);   // 扣點後、寫入前取歷史
    await _chatService.AddMessageAsync(userId, id, "user", dto.Content);

    StartSse();
    var sb = new StringBuilder();
    try
    {
        var request = _chatService.BuildChatRequest(reading, history, dto.Content);
        await foreach (var delta in _ai.StreamAsync(request, ct))
        {
            sb.Append(delta);
            await WriteSseAsync(new { delta }, ct);
        }
        await _chatService.AddMessageAsync(userId, id, "assistant", sb.ToString());
        await WriteSseAsync(new { done = true }, ct);
    }
    catch (OperationCanceledException)
    {
        if (sb.Length > 0) await _chatService.AddMessageAsync(userId, id, "assistant", sb.ToString());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "AI chat failed for reading {ReadingId}", id);
        await _credits.GrantAsync(userId, CreditService.ChatCost, "refund",
            $"refund:{idemKey}", refType: "reading", refId: id.ToString());
        await WriteSseAsync(new { error = "AI 服務暫時無法使用，這則訊息的點數已退回", code = "AI_UNAVAILABLE" }, CancellationToken.None);
    }
    return new EmptyResult();
}
```

## 3. 前端

### 3.1 Proxy
- `src/routes/api/readings/[id]/chat/+server.ts`：照 M1 interpret proxy，多了轉發 JSON body（`request.json()` 後帶上）。
- `src/routes/api/readings/[id]/messages/+server.ts`：GET，普通 JSON 轉發。

### 3.2 `ReadingDisplay.svelte` 解讀下方加對話區
- 有 `interpretation` 才顯示；載入時 GET messages 帶入歷史。
- 送出：`clientMessageId = crypto.randomUUID()`，先把使用者訊息 optimistic 加進列表，POST 後用 `readSse` 把 assistant 訊息逐字 append 到新氣泡。
- 送出中 disabled；402 → 顯示點數不足導引（同 M2）；`{"error"}` → 顯示「已退點，請重試」。
- 樣式：簡單雙欄氣泡（user 靠右、assistant 靠左），不要引入 UI 套件。

## 4. 驗證
- `dotnet build`、`pnpm check` 0 error。
- 瀏覽器：解讀完成 → 追問「那我該先做什麼？」→ 逐字回覆、餘額 -1 → 重整後對話還在。
- 未解讀的 reading 打 chat → 400 `INTERPRET_FIRST`。
- 同一 `clientMessageId` 重送 → 409 且只扣一次（curl 驗）。
- 連續追問 3 則，`reading_messages` 有 6 筆（3 user + 3 assistant）。

## 5. 禁區
- 不做舊訊息 AI 摘要（先用「只帶最近 20 則」硬上限）。
- 不做記憶注入（M5 再把 `memorySummary` 接上）。
- 不做刪除單則訊息、不做匿名追問。

## 6. 常見坑
- 歷史訊息的第一則必須是 `user` role（Claude 要求）——本設計第一則永遠是 readingContext（user），安全；但**不要**把 assistant 存檔訊息排在它前面。
- 扣點在寫入使用者訊息**之前**：失敗（402/409）時不會留下孤兒訊息。
- AI 失敗時使用者訊息保留、點數退回——重送要用**新的** `clientMessageId`（前端重試邏輯記得換）。

## 7. Definition of Done
- [ ] 009 已套用；`dotnet build`/`pnpm check` 過
- [ ] 多輪追問串流正常、留存、重整可見；扣點/退點/冪等如上驗證全過
- [ ] `CLAUDE.md` API 表加 chat / messages 兩條
- [ ] 跑過 `/qa`
