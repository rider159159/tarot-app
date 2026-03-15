# DTO 模式與資料流 — 給 Node.js 開發者

> 在 Node.js 裡，API 回傳的資料結構通常是「隨手組出來的物件」——從資料庫撈出來，挑幾個欄位、展開一下就丟回去。C# 的 DTO 模式把這件事變成了**有型別、有文件、有邊界**的正式合約。

---

## 1. 什麼是 DTO (Data Transfer Object)

DTO 是一個**專門用來在層與層之間傳遞資料的類別**。它不包含商業邏輯，只描述資料的形狀。

| 概念 | Node.js 的做法 | C# 的做法 |
|------|---------------|-----------|
| API 回傳結構 | 直接回傳 plain object | 定義 `ResponseDto` class |
| 限制輸入欄位 | 手動 pick / destructure | 定義 `CreateDto` class，只開放需要的屬性 |
| 文件化 | 靠 JSDoc 或 Swagger 外掛 | 型別本身就是文件，Swagger 自動生成 |
| 重構安全性 | 改欄位名要全域搜尋 | 編譯器直接報錯 |

**核心價值：把「資料庫長什麼樣」和「API 回傳什麼」徹底分開。**

---

## 2. Entity vs DTO — 為什麼不能直接回傳 Entity

### Entity（內部，對應資料庫）

這是本專案的 `Reading` Entity，直接映射到 PostgreSQL 的 `readings` 表：

```csharp
// backend/TarotApi/Models/Reading.cs
public class Reading
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }          // 不該暴露給其他使用者！
    public string SpreadType { get; set; } = string.Empty;
    public string? Question { get; set; }
    public JsonDocument Cards { get; set; } = null!;  // 原始 JSONB，對前端不友善
    public string? Interpretation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Response DTO（外部，API 合約）

```csharp
// backend/TarotApi/Models/Dtos/ReadingResponseDto.cs
public class ReadingResponseDto
{
    public Guid Id { get; set; }
    public string SpreadType { get; set; } = string.Empty;
    public string? Question { get; set; }
    public List<DrawnCardDto> Cards { get; set; } = [];   // 完整解析過的牌資料
    public string? Interpretation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**差異重點：**

| Entity | DTO |
|--------|-----|
| 有 `UserId`（敏感資訊） | 沒有 `UserId` |
| `Cards` 是 `JsonDocument`（原始 JSONB） | `Cards` 是 `List<DrawnCardDto>`（解析過的結構化資料） |
| 服務於 EF Core / 資料庫 | 服務於 API 回應 / 前端 |

### Node.js 通常怎麼做

```javascript
// Node.js — 用 destructuring 去掉不想暴露的欄位
const { userId, cards: rawCards, ...rest } = reading;
return {
  ...rest,
  cards: rawCards.map(c => resolveCard(c))  // 手動轉換
};
```

問題在於：這個「形狀」沒有被任何型別系統約束。如果某天 `rawCards` 的結構改了，你不會在編譯時發現。

---

## 3. 本專案的 DTO 設計

### 輸入 DTO（Client → Server）

輸入 DTO 的職責是**限制客戶端能送什麼**，只暴露必要欄位：

```csharp
// backend/TarotApi/Models/Dtos/ReadingCreateDto.cs
public class ReadingCreateDto
{
    public SpreadType SpreadType { get; set; }  // enum，不是任意字串
    public string? Question { get; set; }
}
```

```csharp
// backend/TarotApi/Models/Dtos/ProfileUpdateDto.cs
public class ProfileUpdateDto
{
    public string DisplayName { get; set; } = string.Empty;
}
```

注意 `ReadingCreateDto` 只接受 `SpreadType` 和 `Question`。客戶端不能指定 `Id`、`UserId`、`CreatedAt` ——這些由伺服器決定。

Node.js 等價做法：

```javascript
// 通常靠 validation middleware (如 joi, zod) 來限制
const schema = z.object({
  spreadType: z.enum(['Single', 'ThreeCardTime', ...]),
  question: z.string().optional(),
});
```

C# 的優勢：型別檢查發生在**編譯時**，不需要額外的 runtime validation library。ASP.NET Core 的 model binding 會自動把 JSON body 反序列化為 DTO，型別不符直接回 400。

### 輸出 DTO（Server → Client）

| DTO | 用途 |
|-----|------|
| `ReadingResponseDto` | 完整占卜紀錄，含解析過的牌資料 |
| `DrawnCardDto` | 單張牌：名稱、正逆位含義、位置資訊 |
| `ProfileDto` | 公開的使用者資訊 |
| `ReadingStatsDto` | 統計聚合資料（含巢狀 DTO） |
| `TarotCardSummaryDto` | 牌列表（摘要欄位） |
| `TarotCardDetailDto` | 牌詳情（完整含義、關鍵字） |

其中 `DrawnCardDto` 的欄位很豐富，因為它是從多個來源**組合**出來的：

```csharp
// backend/TarotApi/Models/Dtos/ReadingResponseDto.cs
public class DrawnCardDto
{
    public string CardId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameCht { get; set; } = string.Empty;
    public string Arcana { get; set; } = string.Empty;
    public string? Suit { get; set; }
    public string Orientation { get; set; } = string.Empty; // "upright" | "reversed"
    public string Meaning { get; set; } = string.Empty;     // 根據正逆位解析後的含義
    public string[] Keywords { get; set; } = [];
    public int PositionIndex { get; set; }
    public string PositionLabel { get; set; } = string.Empty;
    public string PositionDescription { get; set; } = string.Empty;
}
```

這張 DTO 裡的資料來自三個地方：
- **牌資料**（`TarotCardInfo`）→ `CardId`, `Name`, `NameCht`, `Arcana`, `Suit`, `Keywords`
- **抽牌結果** → `Orientation`（正/逆位）
- **牌陣位置**（`SpreadPosition`）→ `PositionIndex`, `PositionLabel`, `PositionDescription`
- **計算欄位** → `Meaning`（根據 `Orientation` 決定取 `MeaningUpright` 還是 `MeaningReversed`）

### 錯誤 DTO

```csharp
// backend/TarotApi/Models/Dtos/ErrorResponseDto.cs
public record ErrorResponseDto
{
    public string Error { get; init; } = string.Empty;
    public string? Code { get; init; }
}
```

注意這裡用了 `record` 而不是 `class`——因為錯誤回應是**不可變的值物件**，用 `record` 更語義化（參見 06 文件中的 record 說明）。

這個 DTO 在整個專案中被一致使用：

```csharp
// Controller 中的使用方式
return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
return Conflict(new ErrorResponseDto { Error = ex.Message, Code = "WEEKLY_LIMIT" });
```

```csharp
// Middleware 中的使用方式
// backend/TarotApi/Middleware/ExceptionHandlingMiddleware.cs
var response = new ErrorResponseDto { Error = error, Code = code };
await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
```

統一的錯誤格式讓前端可以用同一套邏輯處理所有錯誤回應。

---

## 4. Summary vs Detail DTO 模式

同一份資料，列表 API 和詳情 API 回傳不同深度的資訊。看 `TarotController` 的實際程式碼：

### 列表端點 → `TarotCardSummaryDto`（輕量）

```csharp
// backend/TarotApi/Controllers/TarotController.cs
[HttpGet("cards")]
public ActionResult<List<TarotCardSummaryDto>> GetAllCards()
{
    var cards = TarotCards.All.Select(c => new TarotCardSummaryDto
    {
        Id = c.Id,
        Name = c.Name,
        NameCht = c.NameCht,
        Arcana = c.Arcana,
        Suit = c.Suit,
        Number = c.Number
    }).ToList();

    return Ok(cards);
}
```

```csharp
// backend/TarotApi/Models/Dtos/TarotCardSummaryDto.cs
public class TarotCardSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameCht { get; set; } = string.Empty;
    public string Arcana { get; set; } = string.Empty;
    public string? Suit { get; set; }
    public int Number { get; set; }
}
```

### 詳情端點 → `TarotCardDetailDto`（完整）

```csharp
// backend/TarotApi/Controllers/TarotController.cs
[HttpGet("cards/{id}")]
public ActionResult<TarotCardDetailDto> GetCardById(string id)
{
    var card = TarotCards.GetById(id);
    if (card is null)
        return NotFound(new ErrorResponseDto { Error = "找不到該塔羅牌", Code = "NOT_FOUND" });

    return Ok(new TarotCardDetailDto
    {
        Id = card.Id,
        Name = card.Name,
        NameCht = card.NameCht,
        Arcana = card.Arcana,
        Suit = card.Suit,
        Number = card.Number,
        MeaningUpright = card.MeaningUpright,    // Detail 才有
        MeaningReversed = card.MeaningReversed,  // Detail 才有
        Keywords = card.Keywords                 // Detail 才有
    });
}
```

```csharp
// backend/TarotApi/Models/Dtos/TarotCardDetailDto.cs
public class TarotCardDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameCht { get; set; } = string.Empty;
    public string Arcana { get; set; } = string.Empty;
    public string? Suit { get; set; }
    public int Number { get; set; }
    public string MeaningUpright { get; set; } = string.Empty;   // 額外欄位
    public string MeaningReversed { get; set; } = string.Empty;  // 額外欄位
    public string[] Keywords { get; set; } = [];                 // 額外欄位
}
```

### 為什麼要分兩個 DTO？

- **GET /api/tarot/cards** 回傳 78 張牌，如果每張都帶完整的 `MeaningUpright`、`MeaningReversed`（每個都是一長段中文），回應會大很多
- 列表頁只需要顯示牌名和分類，不需要含義
- 點進去看詳情才載入完整資料

### Node.js 等價模式

```javascript
// Node.js 通常用 GraphQL field selection
const cards = await query(`{ cards { id name nameCht } }`);

// 或手動 pick
const summary = cards.map(({ id, name, nameCht, arcana, suit, number }) =>
  ({ id, name, nameCht, arcana, suit, number })
);
```

C# 沒有 GraphQL 那種動態 field selection（除非你特別引入），所以用 Summary/Detail 兩個 DTO 來達到類似效果。

---

## 5. 完整資料流範例一：建立占卜 (POST /api/readings)

從 HTTP request 進來到 response 出去，逐步追蹤資料如何流動和轉換。

### Step 1: 客戶端發送請求

```
POST /api/readings
Authorization: Bearer eyJhbGciOiJFUzI1NiIs...
Content-Type: application/json

{
  "spreadType": "ThreeCardTime",
  "question": "我的職涯方向？"
}
```

### Step 2: ASP.NET Core Middleware Pipeline

```
HTTP Request
  → ExceptionHandlingMiddleware   (包 try/catch，統一錯誤格式)
    → CORS validation             (檢查 Origin 是否在 ALLOWED_ORIGINS)
      → JWT Authentication        (驗證 token，填入 User claims)
        → Authorization            (檢查 [Authorize] attribute)
          → ReadingController      (到達 Controller)
```

### Step 3: Controller 接收並委派

```csharp
// backend/TarotApi/Controllers/ReadingController.cs
[HttpPost]
public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
{
    var userId = User.GetUserId();  // 從 JWT 的 sub claim 取出 Guid
    var result = await readingService.CreateReading(userId, dto.SpreadType, dto.Question);
    return Created($"/api/readings/{result.Id}", result);
}
```

這裡發生的事：
1. `[FromBody]` → ASP.NET 自動把 JSON body 反序列化為 `ReadingCreateDto`
2. `"ThreeCardTime"` 字串自動轉為 `SpreadType.ThreeCardTime` enum（因為 `SpreadType` 上有 `[JsonConverter(typeof(JsonStringEnumConverter))]`）
3. `User.GetUserId()` → 從 JWT token 的 claims 中提取使用者 ID

### Step 4: Service 層處理商業邏輯

```csharp
// backend/TarotApi/Services/ReadingService.cs
public async Task<ReadingResponseDto> CreateReading(Guid userId, SpreadType spreadType, string? question)
{
    // 4a. 抽牌
    var drawnCards = tarotService.DrawCards(spreadType);

    // 4b. 建構 JSONB 格式（精簡格式，只存 ID 和狀態）
    var cardsPayload = drawnCards.Select(dc => new
    {
        card_id = dc.Card.Id,
        orientation = dc.Orientation,
        position_index = dc.Position.Index
    });

    // 4c. 建立 Entity 寫入資料庫
    var reading = new Reading
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        SpreadType = SpreadTypeToString(spreadType),  // enum → "three-card-time"
        Question = question,
        Cards = JsonDocument.Parse(JsonSerializer.Serialize(cardsPayload)),
        CreatedAt = DateTime.UtcNow
    };

    db.Readings.Add(reading);
    await db.SaveChangesAsync();

    // 4d. Entity → DTO 轉換
    return ToResponseDto(reading, drawnCards);
}
```

### Step 4a 細節：抽牌過程

```csharp
// backend/TarotApi/Services/TarotService.cs
public List<DrawnCardResult> DrawCards(SpreadType spreadType)
{
    var positions = SpreadConfigs[spreadType];
    var allCards = TarotCards.All;

    // Fisher-Yates shuffle，使用密碼學安全的亂數
    var indices = Enumerable.Range(0, allCards.Count).ToArray();
    for (var i = indices.Length - 1; i > 0; i--)
    {
        var j = RandomNumberGenerator.GetInt32(i + 1);
        (indices[i], indices[j]) = (indices[j], indices[i]);
    }

    // 取前 N 張 + 感受牌
    var results = new List<DrawnCardResult>(totalCards);
    for (var i = 0; i < positions.Length; i++)
    {
        var card = allCards[indices[i]];
        var orientation = RandomNumberGenerator.GetInt32(2) == 0 ? "upright" : "reversed";
        results.Add(new DrawnCardResult(card, orientation, positions[i]));
    }

    // 非 Single 牌陣加上感受牌
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

`DrawnCardResult` 是一個 `record`，打包了牌資訊、正逆位、位置：

```csharp
public record DrawnCardResult(TarotCardInfo Card, string Orientation, SpreadPosition Position);
```

### Step 4d 細節：Entity → DTO 轉換

```csharp
// backend/TarotApi/Services/ReadingService.cs
private static ReadingResponseDto ToResponseDto(Reading reading, List<TarotService.DrawnCardResult> drawnCards)
{
    return new ReadingResponseDto
    {
        Id = reading.Id,
        SpreadType = reading.SpreadType,
        Question = reading.Question,
        Interpretation = reading.Interpretation,
        Notes = reading.Notes,
        CreatedAt = reading.CreatedAt,
        Cards = drawnCards.Select(dc => new DrawnCardDto
        {
            CardId = dc.Card.Id,
            Name = dc.Card.Name,
            NameCht = dc.Card.NameCht,
            Arcana = dc.Card.Arcana,
            Suit = dc.Card.Suit,
            Orientation = dc.Orientation,
            Meaning = dc.Orientation == "upright"
                ? dc.Card.MeaningUpright
                : dc.Card.MeaningReversed,  // 根據正逆位決定含義
            Keywords = dc.Card.Keywords,
            PositionIndex = dc.Position.Index,
            PositionLabel = dc.Position.Label,
            PositionDescription = dc.Position.Description
        }).ToList()
    };
}
```

### Step 5: 最終 JSON 回應

```
HTTP/1.1 201 Created
Location: /api/readings/a1b2c3d4-...

{
  "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "spreadType": "three-card-time",
  "question": "我的職涯方向？",
  "cards": [
    {
      "cardId": "major_00_fool",
      "name": "The Fool",
      "nameCht": "愚者",
      "arcana": "major",
      "suit": null,
      "orientation": "upright",
      "meaning": "新的開始、冒險、自由、天真無邪、無限可能。代表一段旅程的起點...",
      "keywords": ["新開始", "冒險", "自由", "天真", "可能性"],
      "positionIndex": 0,
      "positionLabel": "過去",
      "positionDescription": "影響當前情況的過去因素"
    },
    {
      "cardId": "major_01_magician",
      "name": "The Magician",
      "nameCht": "魔術師",
      "arcana": "major",
      "suit": null,
      "orientation": "reversed",
      "meaning": "操縱、欺騙、能力未發揮、缺乏方向...",
      "keywords": ["創造力", "意志力", "自信", "技能", "行動"],
      "positionIndex": 1,
      "positionLabel": "現在",
      "positionDescription": "目前的狀態與挑戰"
    },
    {
      "cardId": "minor_cups_01_ace",
      "name": "Ace of Cups",
      "nameCht": "聖杯王牌",
      "arcana": "minor",
      "suit": "cups",
      "orientation": "upright",
      "meaning": "...",
      "keywords": ["..."],
      "positionIndex": 2,
      "positionLabel": "未來",
      "positionDescription": "如果沿著目前道路前進的可能發展"
    },
    {
      "cardId": "major_17_star",
      "name": "The Star",
      "nameCht": "星星",
      "arcana": "major",
      "suit": null,
      "orientation": "upright",
      "meaning": "...",
      "keywords": ["..."],
      "positionIndex": 3,
      "positionLabel": "你的感受",
      "positionDescription": "你對此問題最真實的內心感受"
    }
  ],
  "interpretation": null,
  "notes": null,
  "createdAt": "2026-03-15T10:30:00Z"
}
```

### 資料流視覺化

```
ReadingCreateDto          Reading (Entity)         ReadingResponseDto
┌──────────────┐    ┌──────────────────┐    ┌────────────────────┐
│ SpreadType   │    │ Id (new Guid)    │    │ Id                 │
│ Question?    │    │ UserId           │    │ SpreadType         │
└──────────────┘    │ SpreadType       │    │ Question           │
      │             │ Question         │    │ Cards: [           │
      ▼             │ Cards (JSONB)    │    │   DrawnCardDto {   │
  Controller        │ CreatedAt        │    │     CardId         │
  extracts userId   └──────────────────┘    │     Name, NameCht  │
  from JWT                │                 │     Orientation    │
      │                   │                 │     Meaning ←──── 根據正逆位解析
      ▼                   │                 │     Keywords       │
  Service                 │                 │     PositionLabel  │
  draws cards,            ▼                 │   }                │
  builds Entity      SaveChanges()          │ ]                  │
      │                                     │ CreatedAt          │
      ▼                                     └────────────────────┘
  ToResponseDto()                                    │
  Entity + DrawnCards → DTO                          ▼
                                              JSON Response
```

---

## 6. 完整資料流範例二：取得統計 (GET /api/readings/stats)

這個範例展示**聚合查詢**如何產生巢狀 DTO。

### Step 1: 請求

```
GET /api/readings/stats
Authorization: Bearer eyJhbGciOiJFUzI1NiIs...
```

### Step 2: Controller

```csharp
// backend/TarotApi/Controllers/ReadingController.cs
[HttpGet("stats")]
public async Task<ActionResult<ReadingStatsDto>> GetStats()
{
    var userId = User.GetUserId();
    var stats = await readingService.GetStats(userId);
    return Ok(stats);
}
```

### Step 3: Service — 多種查詢策略組合

```csharp
// backend/TarotApi/Services/ReadingService.cs
public async Task<ReadingStatsDto> GetStats(Guid userId)
{
    var userReadings = db.Readings.Where(r => r.UserId == userId);

    // LINQ: 總次數
    var totalCount = await userReadings.CountAsync();

    // LINQ: 最後一次占卜時間
    var lastReadingAt = totalCount > 0
        ? await userReadings.MaxAsync(r => (DateTime?)r.CreatedAt)
        : null;

    // LINQ GroupBy: 各牌陣使用次數
    var spreadUsage = await userReadings
        .GroupBy(r => r.SpreadType)
        .Select(g => new SpreadStatDto { SpreadType = g.Key, Count = g.Count() })
        .ToListAsync();

    // Raw SQL: 用 jsonb_array_elements() 展開 JSONB 陣列，統計各牌出現次數
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

    // 把 card_id 對應到中文名稱
    var topCardDtos = topCards.Select(tc =>
    {
        var cardInfo = TarotCards.GetById(tc.CardId);
        return new CardStatDto
        {
            CardId = tc.CardId,
            NameCht = cardInfo?.NameCht ?? tc.CardId,
            Count = tc.Count
        };
    }).ToList();

    return new ReadingStatsDto
    {
        TotalCount = totalCount,
        TopCards = topCardDtos,
        SpreadUsage = spreadUsage,
        LastReadingAt = lastReadingAt
    };
}
```

### 巢狀 DTO 結構

```csharp
// backend/TarotApi/Models/Dtos/ReadingStatsDto.cs
public class ReadingStatsDto
{
    public int TotalCount { get; set; }
    public List<CardStatDto> TopCards { get; set; } = [];      // 巢狀 DTO
    public List<SpreadStatDto> SpreadUsage { get; set; } = []; // 巢狀 DTO
    public DateTime? LastReadingAt { get; set; }
}

public class CardStatDto
{
    public string CardId { get; set; } = string.Empty;
    public string NameCht { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SpreadStatDto
{
    public string SpreadType { get; set; } = string.Empty;
    public int Count { get; set; }
}
```

### Step 4: JSON 回應

```json
{
  "totalCount": 42,
  "topCards": [
    { "cardId": "major_00_fool", "nameCht": "愚者", "count": 8 },
    { "cardId": "major_01_magician", "nameCht": "魔術師", "count": 6 },
    { "cardId": "minor_cups_01_ace", "nameCht": "聖杯王牌", "count": 5 },
    { "cardId": "major_02_high_priestess", "nameCht": "女祭司", "count": 4 },
    { "cardId": "major_17_star", "nameCht": "星星", "count": 4 }
  ],
  "spreadUsage": [
    { "spreadType": "three-card-time", "count": 15 },
    { "spreadType": "single", "count": 12 },
    { "spreadType": "celtic-cross", "count": 8 },
    { "spreadType": "three-card-problem", "count": 5 },
    { "spreadType": "weekly-fortune", "count": 2 }
  ],
  "lastReadingAt": "2026-03-15T10:30:00Z"
}
```

### 這個範例的教學重點

1. **LINQ vs Raw SQL**：簡單的聚合用 LINQ（`CountAsync`, `GroupBy`），複雜的 JSONB 操作用 Raw SQL
2. **中間型別 `CardStatRaw`**：Raw SQL 查詢結果先映射到一個 private class，再轉換成公開的 `CardStatDto`（加上 `NameCht`）
3. **巢狀 DTO**：`ReadingStatsDto` 裡包含 `List<CardStatDto>` 和 `List<SpreadStatDto>`，序列化後自然變成巢狀 JSON

---

## 7. 對比 Node.js 的資料轉換方式

### Node.js 做法

```javascript
// 手動 mapping function
function toResponseDto(reading, drawnCards) {
  return {
    id: reading.id,
    spreadType: reading.spread_type,
    question: reading.question,
    cards: drawnCards.map(dc => ({
      cardId: dc.card.id,
      name: dc.card.name,
      nameCht: dc.card.nameCht,
      orientation: dc.orientation,
      meaning: dc.orientation === 'upright'
        ? dc.card.meaningUpright
        : dc.card.meaningReversed,
      keywords: dc.card.keywords,
      positionIndex: dc.position.index,
      positionLabel: dc.position.label,
      positionDescription: dc.position.description,
    })),
    createdAt: reading.created_at,
  };
}
```

### C# 做法（本專案實際程式碼）

```csharp
private static ReadingResponseDto ToResponseDto(Reading reading, List<TarotService.DrawnCardResult> drawnCards)
{
    return new ReadingResponseDto
    {
        Id = reading.Id,
        SpreadType = reading.SpreadType,
        Question = reading.Question,
        Interpretation = reading.Interpretation,
        Notes = reading.Notes,
        CreatedAt = reading.CreatedAt,
        Cards = drawnCards.Select(dc => new DrawnCardDto
        {
            CardId = dc.Card.Id,
            Name = dc.Card.Name,
            NameCht = dc.Card.NameCht,
            Arcana = dc.Card.Arcana,
            Suit = dc.Card.Suit,
            Orientation = dc.Orientation,
            Meaning = dc.Orientation == "upright"
                ? dc.Card.MeaningUpright
                : dc.Card.MeaningReversed,
            Keywords = dc.Card.Keywords,
            PositionIndex = dc.Position.Index,
            PositionLabel = dc.Position.Label,
            PositionDescription = dc.Position.Description
        }).ToList()
    };
}
```

### 關鍵差異

| 面向 | Node.js | C# |
|------|---------|-----|
| 回傳型別 | `any` 或手寫 interface | `ReadingResponseDto`（編譯器強制） |
| 欄位打錯字 | Runtime 才發現（或不會發現） | 編譯時直接報錯 |
| 漏了欄位 | Silently `undefined` | 用預設值或編譯警告 |
| 多了欄位 | 不小心就外洩了 | DTO 沒定義的屬性根本無法設定 |
| Intellisense | 要靠 JSDoc / TypeScript | 原生支援，自動完成 |
| 重構 | 改欄位名要 grep | IDE 一鍵 rename，編譯器確認 |

---

## 8. 重要模式總結

### DTO 的四種角色

```
Client                     Server                      Database
  │                          │                            │
  │   ReadingCreateDto       │                            │
  │ ──────────────────────→  │                            │
  │   (限制輸入)             │     Reading (Entity)       │
  │                          │ ────────────────────────→   │
  │                          │     (映射資料庫)           │
  │                          │ ←────────────────────────   │
  │   ReadingResponseDto     │                            │
  │ ←──────────────────────  │                            │
  │   (控制輸出)             │                            │
  │                          │                            │
  │   ErrorResponseDto       │                            │
  │ ←──────────────────────  │                            │
  │   (統一錯誤格式)         │                            │
```

### 設計原則

1. **Input DTO（輸入）**：限制客戶端能送什麼。永遠不要讓客戶端能設定 `Id`、`UserId`、`CreatedAt` 等伺服器控制的欄位。

2. **Output DTO（輸出）**：控制 API 回傳什麼。永遠不要直接回傳 Entity，因為它可能包含敏感欄位（如 `UserId`）或不友善的格式（如 `JsonDocument`）。

3. **Entity（實體）**：只對應資料庫結構。Entity 是 EF Core 的領域，DTO 是 API 的領域，兩者不應混用。

4. **Mapping（轉換）**：Service 層負責 Entity 和 DTO 之間的轉換。本專案用 `ToResponseDto()` 這類 private static method。大型專案可能會用 AutoMapper 等函式庫。

5. **型別安全**：每個 DTO 都有明確的 class 定義。改了 DTO 的屬性名，所有使用它的地方都會在編譯時報錯——不用擔心漏改。

### 本專案 DTO 檔案一覽

```
backend/TarotApi/Models/
├── Reading.cs              ← Entity
├── Profile.cs              ← Entity
├── SpreadType.cs           ← Enum
├── SpreadPosition.cs       ← Value Object (record)
└── Dtos/
    ├── ReadingCreateDto.cs     ← 輸入：建立占卜
    ├── ReadingResponseDto.cs   ← 輸出：占卜結果（含 DrawnCardDto）
    ├── ReadingStatsDto.cs      ← 輸出：統計資料（含 CardStatDto, SpreadStatDto）
    ├── ProfileDto.cs           ← 輸出：使用者資料
    ├── ProfileUpdateDto.cs     ← 輸入：更新使用者名稱
    ├── TarotCardSummaryDto.cs  ← 輸出：牌列表（摘要）
    ├── TarotCardDetailDto.cs   ← 輸出：牌詳情（完整）
    └── ErrorResponseDto.cs     ← 輸出：統一錯誤格式
```
