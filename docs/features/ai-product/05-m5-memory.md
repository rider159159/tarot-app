# M5｜跨次記憶 Soul Profile

> 目標：滾動摘要式記憶——每次解讀後非同步用便宜模型更新一段 300~500 字摘要；之後的解讀/追問把摘要注入 prompt（「你已知這位使用者…」）；個人頁可查看/清除。
> 前置：M1（建議 M3 也完成，追問才能吃到記憶）。

## 1. Migration `010_user_memory.sql`

```sql
-- 010_user_memory.sql
CREATE TABLE user_memory (
  user_id    uuid PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  summary    text NOT NULL DEFAULT '',
  updated_at timestamptz NOT NULL DEFAULT now(),
  version    int NOT NULL DEFAULT 0        -- 樂觀鎖
);
ALTER TABLE user_memory ENABLE ROW LEVEL SECURITY;
```

（規格書原有 `themes jsonb` 先不做——摘要文字就夠用，少一個要維護的結構。）

## 2. 後端

### 2.1 Entity `Models/UserMemory.cs` ＋ `DbSet<UserMemory> UserMemories`（照舊對映）。

### 2.2 新檔 `Services/MemoryService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using TarotApi.Data;
using TarotApi.Services.Ai;

namespace TarotApi.Services;

public class MemoryService(TarotDbContext db, IAiClient ai, AiModelOptions models,
    ILogger<MemoryService> logger)
{
    private const string UpdateSystemPrompt = """
        你在維護一位塔羅使用者的長期背景摘要。輸入是「舊摘要」與「這次占卜的內容」。
        輸出新的摘要：300~500 字，繁體中文。
        內容涵蓋：反覆出現的主題、在意的人事物、情緒模式、重要背景。
        規則：合併去重、保留仍然有效的舊資訊、移除過時或一次性的細節；只輸出摘要本文。
        """;

    public async Task<string?> GetSummaryAsync(Guid userId)
    {
        var m = await db.UserMemories.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
        return string.IsNullOrWhiteSpace(m?.Summary) ? null : m.Summary;
    }

    public async Task ClearAsync(Guid userId) =>
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM user_memory WHERE user_id = {userId}");

    /// <summary>用「舊摘要＋這次占卜重點」重算摘要。呼叫端負責放到背景執行。</summary>
    public async Task UpdateAsync(Guid userId, string latestReadingDigest, CancellationToken ct = default)
    {
        var existing = await db.UserMemories.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var oldSummary = existing?.Summary ?? "（尚無舊摘要）";
        var oldVersion = existing?.Version ?? -1;

        var user = $"【舊摘要】\n{oldSummary}\n\n【這次占卜】\n{latestReadingDigest}\n\n請輸出新的摘要。";
        var request = new AiRequest(models.Light, UpdateSystemPrompt,
            [new AiMessage("user", user)], MaxTokens: 800);
        var newSummary = await ai.CompleteAsync(request, ct);
        if (newSummary.Length > 2000) newSummary = newSummary[..2000];

        // upsert + 樂觀鎖：版本變了代表併發更新，放棄這次（下次解讀會再算）
        var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO user_memory (user_id, summary, version, updated_at)
            VALUES ({userId}, {newSummary}, 0, now())
            ON CONFLICT (user_id) DO UPDATE
              SET summary = {newSummary}, version = user_memory.version + 1, updated_at = now()
              WHERE user_memory.version = {oldVersion}");
        if (affected == 0)
            logger.LogInformation("Memory update skipped (version conflict) for {UserId}", userId);
    }
}
```

註冊：`builder.Services.AddScoped<MemoryService>();`

### 2.3 觸發時機（背景執行，不擋回應）

在 `ReadingController.Interpret` 成功存檔解讀之後（`done` 事件之後）加：

```csharp
FireAndForgetMemoryUpdate(userId, reading, sb.ToString());
```

Controller 內新增（⚠️ 必須開新 scope，不能沿用 request 的 DbContext）：

```csharp
private void FireAndForgetMemoryUpdate(Guid userId, Reading reading, string interpretation)
{
    var digest = $"問題：{reading.Question}\n情境：{reading.Context?.RootElement.GetRawText()}\n解讀重點：{interpretation[..Math.Min(interpretation.Length, 800)]}";
    _ = Task.Run(async () =>
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var memory = scope.ServiceProvider.GetRequiredService<MemoryService>();
            await memory.UpdateAsync(userId, digest);
        }
        catch (Exception ex) { _logger.LogError(ex, "Memory update failed for {UserId}", userId); }
    });
}
```

建構子注入 `IServiceScopeFactory _scopeFactory`。追問（M3）則在**每第 5 則 assistant 訊息後**同樣觸發一次（digest 用最後幾則對話拼）。

### 2.4 注入到解讀與追問

- `Interpret`：呼叫 `BuildInterpretationInput(reading, memorySummary)` 前先 `var memorySummary = await _memoryService.GetSummaryAsync(userId);`
- `Chat`：同樣把 summary 傳進 `BuildChatRequest`。
- `BuildInterpretationInput` 在 M1 已預留參數，不用改。

### 2.5 新檔 `Controllers/MemoryController.cs`

```csharp
[ApiController]
[Route("api/memory")]
public class MemoryController(MemoryService memory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(new { summary = await memory.GetSummaryAsync(User.GetUserId()) });

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        await memory.ClearAsync(User.GetUserId());
        return NoContent();
    }
}
```

## 3. 前端

- Proxy：`src/routes/api/memory/+server.ts`（GET＋DELETE，普通 JSON 轉發）。
- `/profile` 加「AI 記憶」區塊：
  - 顯示 summary（空 → 「還沒有記憶，多占卜幾次我會慢慢認識你」）。
  - 「清除記憶」按鈕 → confirm 對話框 → DELETE → 顯示已清除。
  - 一句隱私說明：「記憶只用於讓解讀更貼合你，隨時可清除；刪除帳號時一併刪除。」

## 4. 驗證
- 解讀一次 → 等幾秒 → `SELECT * FROM user_memory;` 有摘要；後端 log 無錯誤。
- 第二次抽牌（不同問題）解讀 → 解讀內容自然帶到先前脈絡（人工判讀）；`user_memory.version` 遞增。
- `/profile` 看得到摘要；清除後 GET 回 `summary: null`，下次解讀不注入。
- 解讀回應延遲不受影響（記憶更新在背景跑）。

## 5. 禁區
- 不做 themes 結構化標籤、不做週報（之後另開）。
- 不做排程；只有解讀/追問事件觸發。
- 管理端點不分頁不搜尋——就 GET/DELETE 兩個。

## 6. 常見坑
- **背景 Task 不能用 request scope 的 DbContext**（會 ObjectDisposedException）——一定走 `IServiceScopeFactory.CreateScope()`（上面範本已含）。
- 樂觀鎖衝突是正常情況（連續兩次解讀），跳過即可，不要 retry loop。
- 注入 prompt 時 summary 放 system/user 的「參考資訊」位置（M1 模板已有【你已知的這位使用者背景】段），提醒模型「供參考，不必逐條回應」。

## 7. Definition of Done
- [ ] 010 已套用；build/check 過
- [ ] 解讀後記憶自動更新；第二次解讀吃得到；個人頁可看可清
- [ ] `CLAUDE.md` API 表加 GET/DELETE `/api/memory`
- [ ] 跑過 `/qa`
