# Service 層與依賴注入 — 給 Node.js 開發者

## 1. 為什麼要分離 Controller 和 Service

在 Express 專案中，很多人會把邏輯直接寫在 route handler 裡：

```javascript
// Express — 邏輯全寫在 route handler（等同於把所有東西塞在 Controller）
app.post('/api/readings', async (req, res) => {
  const cards = drawCards(req.body.spreadType);       // 業務邏輯
  await db.query('INSERT INTO readings...');           // 資料庫操作
  res.status(201).json(result);                        // HTTP 回應
});
```

在 .NET 中，我們會把**處理 HTTP 的部分**和**業務邏輯**拆開：

- **Controller**：處理 HTTP 相關的事（路由、請求解析、回應狀態碼）
- **Service**：包含業務邏輯（抽牌演算法、資料庫操作、驗證規則）

來看這個專案的 `ReadingController`，它非常薄 — 只負責 HTTP 層的事：

```csharp
// Controllers/ReadingController.cs
[ApiController]
[Route("api/readings")]
public class ReadingController(ReadingService readingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
    {
        var userId = User.GetUserId();
        var result = await readingService.CreateReading(userId, dto.SpreadType, dto.Question);
        return Created($"/api/readings/{result.Id}", result);  // 只管回 HTTP 201
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteReading(Guid id)
    {
        var userId = User.GetUserId();
        var deleted = await readingService.DeleteReading(userId, id);

        if (!deleted)
            return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
        return NoContent();  // 只管回 HTTP 204
    }
}
```

Controller 完全不知道牌怎麼抽、資料怎麼存 — 這些全在 Service 裡。

**好處：**

| 面向 | 全寫在 Controller | 分離 Service |
|------|-------------------|-------------|
| 可測試性 | 需要模擬 HTTP 環境才能測 | Service 可以直接單元測試 |
| 可重用性 | 邏輯綁死在特定 endpoint | 多個 Controller 可以共用同一個 Service |
| 可讀性 | endpoint 越多越難維護 | 每個類別職責明確 |


## 2. 建構子注入 (Constructor Injection)

在 Node.js 中，你用 `require()` 或 `import` 直接引入模組：

```javascript
// Node.js — 直接 import，模組就是依賴
const db = require('../db');
const tarotService = require('./tarotService');

async function createReading(userId, spreadType, question) {
  const cards = tarotService.drawCards(spreadType);
  await db.query('INSERT INTO readings...');
}
```

在 C# 中，依賴不是自己 import 的，而是透過**建構子 (Constructor)** 由 DI 容器自動傳入：

```csharp
// Services/ReadingService.cs
public class ReadingService(TarotDbContext db, TarotService tarotService)
{
    // db 和 tarotService 是透過建構子注入的
    // 不需要 new TarotDbContext() 或 import 任何東西

    public async Task<ReadingResponseDto> CreateReading(Guid userId, SpreadType spreadType, string? question)
    {
        var drawnCards = tarotService.DrawCards(spreadType);  // 使用注入的 service
        db.Readings.Add(reading);                             // 使用注入的 DB context
        await db.SaveChangesAsync();
    }
}
```

這裡 `ReadingService(TarotDbContext db, TarotService tarotService)` 是 C# 的 **Primary Constructor** 語法（C# 12 新增）。等同於：

```csharp
// 傳統寫法（效果完全一樣）
public class ReadingService
{
    private readonly TarotDbContext _db;
    private readonly TarotService _tarotService;

    public ReadingService(TarotDbContext db, TarotService tarotService)
    {
        _db = db;
        _tarotService = tarotService;
    }
}
```

你不需要手動 `new ReadingService(db, tarotService)` — DI 容器會自動幫你做。只要在 `Program.cs` 中註冊過，容器就知道怎麼組裝：

```csharp
// Program.cs — 註冊服務
builder.Services.AddSingleton<TarotService>();
builder.Services.AddScoped<ReadingService>();
builder.Services.AddScoped<ProfileService>();

// EF Core 的 DbContext 也是透過 DI 註冊的
builder.Services.AddDbContext<TarotDbContext>(options =>
    options.UseNpgsql(dbConnectionString));
```

當有人需要 `ReadingService` 時，DI 容器會：
1. 看到它需要 `TarotDbContext` → 建立一個（Scoped）
2. 看到它需要 `TarotService` → 取得那個已建立的 Singleton 實例
3. 把這兩個傳進 `ReadingService` 的建構子
4. 回傳組裝好的 `ReadingService`

整個過程自動完成，你只需要宣告「我需要什麼」。


## 3. TarotService — Singleton 服務

```csharp
builder.Services.AddSingleton<TarotService>();
```

**為什麼用 Singleton？** 這個 Service 沒有任何狀態、不依賴資料庫，只包含純邏輯。建立一次就夠了，所有請求共用同一個實例。

> 對應到 Node.js：一個普通的模組匯出就是天生的 Singleton，因為 `require()` 會快取模組。

### SpreadConfigs — 靜態牌陣配置

```csharp
public class TarotService
{
    // 靜態字典：SpreadType → 每個位置的定義
    private static readonly Dictionary<SpreadType, SpreadPosition[]> SpreadConfigs = new()
    {
        [SpreadType.Single] =
        [
            new(0, "指引", "此刻對你最重要的訊息")
        ],
        [SpreadType.ThreeCardTime] =
        [
            new(0, "過去", "影響當前情況的過去因素"),
            new(1, "現在", "目前的狀態與挑戰"),
            new(2, "未來", "如果沿著目前道路前進的可能發展")
        ],
        [SpreadType.CelticCross] =
        [
            new(0, "現狀", "目前的處境與核心問題"),
            new(1, "挑戰", "當前面臨的阻礙或對立力量"),
            new(2, "潛意識", "內心深處的想法與潛在影響"),
            new(3, "過去", "近期影響事件發展的過去因素"),
            new(4, "可能性", "最佳可能結果或目標"),
            new(5, "近未來", "即將發生的事件或影響"),
            new(6, "自我", "你對這個問題的態度與立場"),
            new(7, "環境", "周圍環境與他人的影響"),
            new(8, "希望與恐懼", "內心的期望或擔憂"),
            new(9, "結果", "最終可能的結果")
        ],
        // ... 其他牌陣
    };
```

`SpreadPosition` 是一個 **record** 型別（不可變的資料結構）：

```csharp
// Models/SpreadPosition.cs
public record SpreadPosition(int Index, string Label, string Description);
```

### DrawnCardResult — 用 record 表示抽牌結果

```csharp
public record DrawnCardResult(TarotCardInfo Card, string Orientation, SpreadPosition Position);
```

record 在 C# 中等同於一個不可變的資料物件。Node.js 中最接近的概念是 `Object.freeze()`，但 record 在型別系統中有更強的保證。

### DrawCards() — 核心抽牌演算法

```csharp
public List<DrawnCardResult> DrawCards(SpreadType spreadType)
{
    var positions = SpreadConfigs[spreadType];
    var allCards = TarotCards.All;

    // Fisher-Yates shuffle — 使用密碼學等級的亂數產生器
    // 比 Math.random() / Random.Shared 更安全，適合需要公平性的場景
    var indices = Enumerable.Range(0, allCards.Count).ToArray();  // [0, 1, 2, ..., 77]
    for (var i = indices.Length - 1; i > 0; i--)
    {
        // RandomNumberGenerator.GetInt32() 使用 OS 的密碼學 RNG
        // 等同於 Node.js 的 crypto.randomInt()
        var j = RandomNumberGenerator.GetInt32(i + 1);
        (indices[i], indices[j]) = (indices[j], indices[i]);  // tuple swap 語法
    }

    // 根據牌陣需要的張數，從洗好的牌中取牌
    var totalCards = spreadType == SpreadType.Single ? positions.Length : positions.Length + 1;
    var results = new List<DrawnCardResult>(totalCards);
    for (var i = 0; i < positions.Length; i++)
    {
        var card = allCards[indices[i]];
        // 50/50 機率決定正位或逆位
        var orientation = RandomNumberGenerator.GetInt32(2) == 0 ? "upright" : "reversed";
        results.Add(new DrawnCardResult(card, orientation, positions[i]));
    }

    // 非單張牌陣會額外加一張「感受牌」
    if (spreadType != SpreadType.Single)
    {
        var feelingCard = allCards[indices[positions.Length]];
        var feelingOrientation = RandomNumberGenerator.GetInt32(2) == 0 ? "upright" : "reversed";
        var feelingPosition = new SpreadPosition(positions.Length, "你的感受", "你對此問題最真實的內心感受");
        results.Add(new DrawnCardResult(feelingCard, feelingOrientation, feelingPosition));
    }

    return results;
}
```

**Node.js 等價寫法：**

```javascript
const crypto = require('crypto');

function drawCards(spreadType) {
  const positions = spreadConfigs[spreadType];
  const allCards = getAllCards();  // 78 張牌

  // Fisher-Yates shuffle with crypto
  const indices = Array.from({ length: 78 }, (_, i) => i);
  for (let i = indices.length - 1; i > 0; i--) {
    const j = crypto.randomInt(i + 1);  // 等同於 RandomNumberGenerator.GetInt32()
    [indices[i], indices[j]] = [indices[j], indices[i]];
  }

  const results = positions.map((pos, i) => ({
    card: allCards[indices[i]],
    orientation: crypto.randomInt(2) === 0 ? 'upright' : 'reversed',
    position: pos,
  }));

  // 非單張加感受牌
  if (spreadType !== 'single') {
    results.push({
      card: allCards[indices[positions.length]],
      orientation: crypto.randomInt(2) === 0 ? 'upright' : 'reversed',
      position: { index: positions.length, label: '你的感受', description: '...' },
    });
  }

  return results;
}
```


## 4. ReadingService — Scoped 服務

```csharp
builder.Services.AddScoped<ReadingService>();
```

**為什麼用 Scoped？** 因為它依賴 `TarotDbContext`（EF Core 的 DbContext），而 DbContext 本身就是 Scoped 的 — 每個 HTTP 請求建立一個，請求結束後自動銷毀。

> 重要規則：Scoped 服務可以依賴 Singleton，但 Singleton 不能依賴 Scoped。否則 Scoped 物件會被意外地跨請求共用。

### 4.1 CreateReading — 抽牌、序列化、存入資料庫

```csharp
public async Task<ReadingResponseDto> CreateReading(Guid userId, SpreadType spreadType, string? question)
{
    // 1. 呼叫 TarotService 抽牌（業務邏輯委派給另一個 Service）
    var drawnCards = tarotService.DrawCards(spreadType);

    // 2. 把抽到的牌轉換成要存進 JSONB 欄位的格式
    var cardsPayload = drawnCards.Select(dc => new
    {
        card_id = dc.Card.Id,
        orientation = dc.Orientation,
        position_index = dc.Position.Index
    });

    // 3. 建立 Reading entity
    var reading = new Reading
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        SpreadType = SpreadTypeToString(spreadType),
        Question = question,
        Cards = JsonDocument.Parse(JsonSerializer.Serialize(cardsPayload)),
        CreatedAt = DateTime.UtcNow
    };

    // 4. 存入資料庫（EF Core 的寫法）
    db.Readings.Add(reading);
    await db.SaveChangesAsync();

    // 5. 轉換成回應 DTO
    return ToResponseDto(reading, drawnCards);
}
```

### 4.2 GetReadings — 分頁查詢 (LINQ)

```csharp
public async Task<(List<ReadingResponseDto> Items, int TotalCount)> GetReadings(
    Guid userId, int page, int pageSize)
{
    var query = db.Readings
        .Where(r => r.UserId == userId)           // WHERE user_id = @userId
        .OrderByDescending(r => r.CreatedAt);     // ORDER BY created_at DESC

    var totalCount = await query.CountAsync();     // SELECT COUNT(*)

    var readings = await query
        .Skip((page - 1) * pageSize)              // OFFSET
        .Take(pageSize)                            // LIMIT
        .ToListAsync();                            // 執行查詢

    var items = readings.Select(r => ToResponseDto(r, ResolveCards(r))).ToList();
    return (items, totalCount);  // 回傳 tuple
}
```

注意回傳型別 `(List<ReadingResponseDto> Items, int TotalCount)` 是 **named tuple** — Node.js 沒有對應語法，你通常會回傳 `{ items, totalCount }` 物件。

### 4.3 GetReadingById / DeleteReading — 所有權檢查

```csharp
public async Task<ReadingResponseDto?> GetReadingById(Guid userId, Guid readingId)
{
    // 同時查 readingId 和 userId，確保只能看到自己的資料
    var reading = await db.Readings
        .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

    return reading is null ? null : ToResponseDto(reading, ResolveCards(reading));
}

public async Task<bool> DeleteReading(Guid userId, Guid readingId)
{
    var reading = await db.Readings
        .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

    if (reading is null) return false;

    db.Readings.Remove(reading);
    await db.SaveChangesAsync();
    return true;
}
```

Service 回傳 `null` 或 `false`，由 Controller 決定要回 404 還是 204。這就是職責分離 — Service 不知道 HTTP 狀態碼的存在。

### 4.4 GetStats — LINQ + Raw SQL 處理 JSONB

```csharp
public async Task<ReadingStatsDto> GetStats(Guid userId)
{
    var userReadings = db.Readings.Where(r => r.UserId == userId);

    var totalCount = await userReadings.CountAsync();
    var lastReadingAt = totalCount > 0
        ? await userReadings.MaxAsync(r => (DateTime?)r.CreatedAt)
        : null;

    // 用 LINQ GroupBy 統計各牌陣使用次數
    var spreadUsage = await userReadings
        .GroupBy(r => r.SpreadType)
        .Select(g => new SpreadStatDto { SpreadType = g.Key, Count = g.Count() })
        .ToListAsync();

    // JSONB 欄位需要用 raw SQL — LINQ 無法處理 PostgreSQL 的 jsonb_array_elements
    var topCards = await db.Database
        .SqlQueryRaw<CardStatRaw>(
            """
            SELECT card->>'card_id' AS CardId, COUNT(*) AS Count
            FROM readings, jsonb_array_elements(cards) AS card
            WHERE user_id = {0}
            GROUP BY card->>'card_id'
            ORDER BY Count DESC
            LIMIT 5
            """, userId)
        .ToListAsync();

    // ... 組裝回應 DTO
}
```

這展示了一個常見的模式：能用 LINQ 就用 LINQ（型別安全、可組合），遇到 LINQ 力有未逮的地方（JSONB 操作）就用 raw SQL。

### 4.5 GetWeeklyFortune / CreateWeeklyFortune — 週次限制

```csharp
public async Task<ReadingResponseDto?> GetWeeklyFortune(Guid userId)
{
    var weekStart = GetCurrentWeekStart();
    var reading = await db.Readings
        .Where(r => r.UserId == userId
                    && r.SpreadType == "weekly-fortune"
                    && r.CreatedAt >= weekStart)       // 只查本週的
        .OrderByDescending(r => r.CreatedAt)
        .FirstOrDefaultAsync();

    return reading is null ? null : ToResponseDto(reading, ResolveCards(reading));
}

public async Task<ReadingResponseDto> CreateWeeklyFortune(Guid userId)
{
    var weekStart = GetCurrentWeekStart();
    var exists = await db.Readings
        .AnyAsync(r => r.UserId == userId
                       && r.SpreadType == "weekly-fortune"
                       && r.CreatedAt >= weekStart);

    if (exists)
        throw new InvalidOperationException("本週已經抽過週運了，請下週再來");

    // 重用 CreateReading — Service 內部方法互相呼叫
    return await CreateReading(userId, SpreadType.WeeklyFortune, "本週週運");
}

private static DateTime GetCurrentWeekStart()
{
    var now = DateTime.UtcNow;
    var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
    return now.AddDays(-diff).Date;  // 本週一 00:00 UTC
}
```

注意 Service 丟出 `InvalidOperationException`，Controller 用 `try/catch` 接住並轉成 HTTP 409 Conflict：

```csharp
// Controller 中
catch (InvalidOperationException ex)
{
    return Conflict(new ErrorResponseDto { Error = ex.Message, Code = "WEEKLY_LIMIT" });
}
```

### 4.6 Helper 方法

**SpreadTypeToString** — Switch expression 把 enum 轉成存入 DB 的字串：

```csharp
private static string SpreadTypeToString(SpreadType type) => type switch
{
    SpreadType.Single => "single",
    SpreadType.ThreeCardTime => "three-card-time",
    SpreadType.ThreeCardProblem => "three-card-problem",
    SpreadType.ThreeCardLinear => "three-card-linear",
    SpreadType.CelticCross => "celtic-cross",
    SpreadType.WeeklyFortune => "weekly-fortune",
    _ => throw new ArgumentOutOfRangeException(nameof(type))
};
```

**ResolveCards** — 從 JSONB 反序列化回 domain 物件，包含 legacy 相容處理：

```csharp
private static List<TarotService.DrawnCardResult> ResolveCards(Reading reading)
{
    var cardsJson = reading.Cards.RootElement;
    var results = new List<TarotService.DrawnCardResult>();

    foreach (var element in cardsJson.EnumerateArray())
    {
        var cardId = element.GetProperty("card_id").GetString()!;
        var orientation = element.GetProperty("orientation").GetString()!;
        var positionIndex = element.GetProperty("position_index").GetInt32();

        var card = TarotCards.GetById(cardId);
        if (card is null) continue;

        // 字串 → enum，注意 "three-card" 是舊資料的相容處理
        var spreadType = reading.SpreadType switch
        {
            "single" => SpreadType.Single,
            "three-card" => SpreadType.ThreeCardTime,     // legacy 相容
            "three-card-time" => SpreadType.ThreeCardTime,
            // ...
            _ => SpreadType.Single
        };

        var positions = TarotService.GetPositions(spreadType);
        var position = positionIndex < positions.Length
            ? positions[positionIndex]
            : new SpreadPosition(positionIndex, "你的感受", "你對此問題最真實的內心感受");

        results.Add(new TarotService.DrawnCardResult(card, orientation, position));
    }

    return results;
}
```


## 5. ProfileService — Scoped 服務

專案中最簡單的 Service，很適合作為入門範例：

```csharp
// Services/ProfileService.cs
public class ProfileService(TarotDbContext db)
{
    public async Task<ProfileDto?> GetProfile(Guid userId)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null) return null;

        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            CreatedAt = profile.CreatedAt
        };
    }

    public async Task<ProfileDto?> UpdateProfile(Guid userId, string displayName)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null) return null;

        profile.DisplayName = displayName;
        profile.UpdatedAt = DateTime.UtcNow;     // 手動更新時間戳
        await db.SaveChangesAsync();              // EF Core 自動追蹤變更，只更新修改的欄位

        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            CreatedAt = profile.CreatedAt
        };
    }
}
```

注意：`UpdateProfile` 不需要呼叫任何 `Update()` 方法。EF Core 的 **Change Tracking** 會自動偵測 `profile.DisplayName = displayName` 這個賦值，在 `SaveChangesAsync()` 時只 UPDATE 被改過的欄位。這和 Node.js 中需要手動組 `UPDATE SET ...` SQL 非常不同。


## 6. Node.js 的依賴管理對比

### Node.js 做法 — 直接 import

```javascript
// services/readingService.js
const db = require('../db');
const tarotService = require('./tarotService');

async function createReading(userId, spreadType, question) {
  const cards = tarotService.drawCards(spreadType);
  await db.query('INSERT INTO readings...');
  return result;
}

module.exports = { createReading };
```

### C# DI 做法 — 建構子注入

```csharp
// Services/ReadingService.cs
public class ReadingService(TarotDbContext db, TarotService tarotService)
{
    public async Task<ReadingResponseDto> CreateReading(Guid userId, SpreadType spreadType, string? question)
    {
        var drawnCards = tarotService.DrawCards(spreadType);
        db.Readings.Add(reading);
        await db.SaveChangesAsync();
        return ToResponseDto(reading, drawnCards);
    }
}
```

### 關鍵差異

| 面向 | Node.js (require/import) | C# (DI Container) |
|------|--------------------------|-------------------|
| 取得依賴 | 直接 import，硬編碼路徑 | 建構子宣告，容器自動傳入 |
| 測試時替換 | `jest.mock('./db')` — 侵入式 | 傳入 mock 物件即可 — 自然 |
| 生命週期 | 模組快取 = 全域 Singleton | Singleton / Scoped / Transient 可選 |
| 循環依賴 | 容易出現，難以 debug | DI 容器啟動時就會報錯 |

### 三種生命週期

```csharp
// Program.cs
builder.Services.AddSingleton<TarotService>();    // 整個應用程式生命週期只建立一次
builder.Services.AddScoped<ReadingService>();      // 每個 HTTP 請求建立一次
builder.Services.AddScoped<ProfileService>();      // 每個 HTTP 請求建立一次
// builder.Services.AddTransient<SomeService>();   // 每次注入都建立新的（本專案未使用）
```

| 生命週期 | 建立時機 | 銷毀時機 | 適用場景 |
|----------|---------|---------|---------|
| **Singleton** | 第一次被請求時 | 應用程式關閉 | 無狀態的純邏輯、設定、快取 |
| **Scoped** | 每次 HTTP 請求 | 請求結束 | DbContext、有狀態的業務服務 |
| **Transient** | 每次注入 | 離開作用域 | 輕量、無共用需求的工具類別 |

用 Node.js 的思維理解：
- **Singleton** = `require()` 快取的模組（天生就是）
- **Scoped** = Express middleware 中 `req.db = new DbPool()` 那樣 per-request 的東西
- **Transient** = 每次呼叫都 `new` 一個新物件
