# Controller 與路由 — 給 Node.js 開發者

## 1. Controller 是什麼

在 Express 中，你會建立 router 檔案來定義路由：

```javascript
// Express: routes/health.js
const router = express.Router();
router.get('/', (req, res) => { ... });
module.exports = router;

// 然後在 app.js 掛載
app.use('/api/health', require('./routes/health'));
```

在 ASP.NET Core 中，路由定義在 **Controller 類別** 裡。每個 Controller 是一個繼承 `ControllerBase` 的 class，搭配屬性（Attributes）來宣告路由規則：

```csharp
[ApiController]                   // 啟用 API 行為（自動驗證 model、錯誤時自動回 400）
[Route("api/[controller]")]       // 路由前綴，[controller] 會被類別名稱取代（去掉 "Controller" 後綴）
public class HealthController : ControllerBase
{
    [HttpGet]                     // 對應 GET /api/health
    public IActionResult Get() { ... }
}
```

關鍵差異：

| 概念 | Express | ASP.NET Core |
|------|---------|-------------|
| 路由定義位置 | router 檔案（函式） | Controller 類別（class + 屬性） |
| 路由掛載 | `app.use('/path', router)` | `[Route("api/[controller]")]` 自動掛載 |
| 自動 400 回應 | 需手動驗證 | `[ApiController]` 自動處理 |
| 依賴注入 | 手動 require/import | 建構子注入（框架自動提供） |

### `[ApiController]` 幫你做了什麼

這個屬性啟用了幾個方便的 API 行為：
- **自動 Model 驗證**：如果 request body 不符合 DTO 定義，自動回傳 400 Bad Request
- **自動參數綁定推斷**：複雜型別預設從 body 綁定，簡單型別從 route/query 綁定
- **問題詳情回應**：錯誤回應使用標準的 ProblemDetails 格式

### `[Route("api/[controller]")]` 怎麼運作

`[controller]` 是一個特殊的 token，框架會自動把它替換成類別名稱去掉 "Controller" 後綴的結果：

| 類別名稱 | `[controller]` 替換為 | 最終路由 |
|----------|---------------------|---------|
| `HealthController` | `health` | `/api/health` |
| `ReadingController` | `reading` | `/api/reading` |
| `ProfileController` | `profile` | `/api/profile` |

注意：你也可以直接寫死路由，不用 `[controller]` token。本專案的 `TarotController` 就是這樣做的：

```csharp
[Route("api/tarot")]  // 直接指定，不用 [controller]（否則會變成 /api/tarot 以外的東西... 等等，其實剛好一樣）
public class TarotController : ControllerBase
```

這裡用 `"api/tarot"` 而非 `"api/[controller]"` 是因為要精確控制路由路徑。

---

## 2. 屬性路由 (Attribute Routing)

Express 用方法呼叫定義路由，ASP.NET Core 用屬性標註定義路由：

| Express | ASP.NET Core | 說明 |
|---------|-------------|------|
| `router.get('/')` | `[HttpGet]` | GET 請求 |
| `router.post('/')` | `[HttpPost]` | POST 請求 |
| `router.put('/')` | `[HttpPut]` | PUT 請求 |
| `router.delete('/:id')` | `[HttpDelete("{id:guid}")]` | DELETE，帶路由參數 |
| `router.get('/cards/:id')` | `[HttpGet("cards/{id}")]` | GET，帶子路徑 |
| `router.get('/stats')` | `[HttpGet("stats")]` | GET，靜態子路徑 |

### 路由約束 (Route Constraints)

ASP.NET Core 允許在路由參數中加入型別約束：

```csharp
[HttpGet("{id:guid}")]   // id 必須是合法的 GUID，否則回 404
[HttpGet("{id:int}")]    // id 必須是整數
[HttpGet("{name:alpha}")]// name 必須是字母
```

本專案中 `ReadingController` 使用 `{id:guid}` 確保 id 是合法的 GUID 格式：

```csharp
[HttpGet("{id:guid}")]
public async Task<ActionResult<ReadingResponseDto>> GetReadingById(Guid id)
```

如果你發送 `GET /api/readings/not-a-guid`，ASP.NET Core 會直接回傳 404，根本不會進到你的方法裡。在 Express 中你需要自己驗證：

```javascript
router.get('/:id', (req, res) => {
    // 需要自己驗證 req.params.id 是否為合法 UUID
    if (!isValidUUID(req.params.id)) return res.status(404).json({ error: 'Not found' });
    // ...
});
```

### 子路徑組合

屬性路由會自動組合 Controller 的 `[Route]` 和方法的 `[HttpXxx]`：

```
Controller: [Route("api/readings")]
  方法: [HttpGet]            → GET  /api/readings
  方法: [HttpPost]           → POST /api/readings
  方法: [HttpGet("{id:guid}")] → GET  /api/readings/{id}
  方法: [HttpGet("stats")]   → GET  /api/readings/stats
  方法: [HttpGet("weekly-fortune")]  → GET  /api/readings/weekly-fortune
  方法: [HttpPost("weekly-fortune")] → POST /api/readings/weekly-fortune
  方法: [HttpDelete("{id:guid}")]    → DELETE /api/readings/{id}
```

---

## 3. 參數綁定 (Parameter Binding)

ASP.NET Core 會自動從 HTTP 請求的不同部位綁定參數到方法參數上。

### 綁定來源對照

| 來源 | ASP.NET Core 屬性 | Express 等價 | 範例 |
|------|-------------------|-------------|------|
| 請求 Body | `[FromBody]` | `req.body` | JSON payload |
| Query String | `[FromQuery]` | `req.query` | `?page=1&pageSize=10` |
| 路由參數 | `[FromRoute]` | `req.params` | `/readings/{id}` |
| HTTP Header | `[FromHeader]` | `req.headers` | `Authorization` |

### 自動推斷規則

有了 `[ApiController]` 屬性，大多數情況下你不需要明確標註來源：
- **簡單型別**（string, int, Guid 等）：從路由或 query string 綁定
- **複雜型別**（class, DTO）：從 request body 綁定

### 本專案的實際範例

**路由參數綁定** — `ReadingController.GetReadingById`：

```csharp
// GET /api/readings/550e8400-e29b-41d4-a716-446655440000
// URL 中的 {id} 自動綁定到方法參數 Guid id
[HttpGet("{id:guid}")]
public async Task<ActionResult<ReadingResponseDto>> GetReadingById(Guid id)
{
    var userId = User.GetUserId();
    var result = await readingService.GetReadingById(userId, id);

    if (result is null)
        return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
    return Ok(result);
}
```

Express 等價：

```javascript
router.get('/:id', async (req, res) => {
    const id = req.params.id;
    const userId = getUserId(req);
    const result = await readingService.getReadingById(userId, id);

    if (!result) return res.status(404).json({ error: '找不到該筆占卜紀錄', code: 'NOT_FOUND' });
    return res.json(result);
});
```

**Query String 綁定** — `ReadingController.GetReadings`：

```csharp
// GET /api/readings?page=2&pageSize=20
// [FromQuery] 明確標註從 query string 綁定，同時提供預設值
[HttpGet]
public async Task<ActionResult> GetReadings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
{
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 50) pageSize = 50;

    var userId = User.GetUserId();
    var (items, totalCount) = await readingService.GetReadings(userId, page, pageSize);

    return Ok(new { items, totalCount, page, pageSize });
}
```

Express 等價：

```javascript
router.get('/', async (req, res) => {
    let page = parseInt(req.query.page) || 1;
    let pageSize = parseInt(req.query.pageSize) || 10;
    if (page < 1) page = 1;
    if (pageSize < 1) pageSize = 10;
    if (pageSize > 50) pageSize = 50;

    const userId = getUserId(req);
    const { items, totalCount } = await readingService.getReadings(userId, page, pageSize);

    return res.json({ items, totalCount, page, pageSize });
});
```

**Request Body 綁定** — `ReadingController.CreateReading`：

```csharp
// POST /api/readings  Body: { "spreadType": "single", "question": "今天運勢？" }
// [FromBody] 標註從 request body 綁定，自動反序列化 JSON 為 ReadingCreateDto
[HttpPost]
public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
{
    var userId = User.GetUserId();
    var result = await readingService.CreateReading(userId, dto.SpreadType, dto.Question);
    return Created($"/api/readings/{result.Id}", result);
}
```

Express 等價：

```javascript
router.post('/', async (req, res) => {
    const { spreadType, question } = req.body;  // 需要 express.json() middleware
    const userId = getUserId(req);
    const result = await readingService.createReading(userId, spreadType, question);

    return res.status(201).location(`/api/readings/${result.id}`).json(result);
});
```

---

## 4. 回傳值與 HTTP 狀態碼

ASP.NET Core 的 `ControllerBase` 提供了一系列 helper 方法來產生 HTTP 回應：

| C# 方法 | HTTP 狀態碼 | Express 等價 | 本專案使用場景 |
|---------|-----------|-------------|--------------|
| `Ok(data)` | 200 OK | `res.json(data)` | 查詢成功 |
| `Created(url, data)` | 201 Created | `res.status(201).json(data)` | 建立占卜紀錄 |
| `NoContent()` | 204 No Content | `res.status(204).send()` | 刪除成功 |
| `NotFound(data)` | 404 Not Found | `res.status(404).json(data)` | 找不到紀錄 |
| `Conflict(data)` | 409 Conflict | `res.status(409).json(data)` | 每週運勢已存在 |

### 本專案的回傳值範例

**200 OK** — 查詢所有塔羅牌：

```csharp
return Ok(cards);  // 自動序列化為 JSON，Content-Type: application/json
```

**201 Created** — 建立占卜紀錄：

```csharp
return Created($"/api/readings/{result.Id}", result);
// 設定 Location header 為新資源的 URL，回傳 201 + body
```

**204 No Content** — 刪除成功（無回傳 body）：

```csharp
return NoContent();
```

**404 Not Found** — 找不到資源：

```csharp
return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
```

**409 Conflict** — 每週運勢已抽過：

```csharp
return Conflict(new ErrorResponseDto { Error = ex.Message, Code = "WEEKLY_LIMIT" });
```

### `ActionResult<T>` vs `IActionResult`

```csharp
// IActionResult：不指定回傳型別，任何狀態碼都可以回
public IActionResult Get() { return Ok(new { status = "ok" }); }

// ActionResult<T>：告訴 Swagger 成功時會回傳 T 型別，也能回 NotFound 等
public ActionResult<ReadingResponseDto> GetReadingById(Guid id) { ... }

// 沒有包裝的 ActionResult：通常用於回傳不固定結構的資料
public async Task<ActionResult> GetReadings(...) { ... }
```

`ActionResult<T>` 的好處是 Swagger/OpenAPI 會自動產生正確的回應 schema 文件。

---

## 5. 認證屬性

### 全域認證 (Global Auth)

本專案在 `Program.cs` 中設定了全域認證過濾器，所有 Controller 的所有端點都需要認證：

```csharp
// backend/TarotApi/Program.cs
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
});
```

這等同於在 Express 中把 auth middleware 掛在最上層：

```javascript
// Express 等價
app.use('/api', authMiddleware);  // 所有 /api 路由都需要認證
```

### `[AllowAnonymous]` — 允許匿名存取

要讓特定端點跳過認證（例如 health check），用 `[AllowAnonymous]`：

```csharp
[HttpGet]
[AllowAnonymous]  // 覆蓋全域認證要求
public IActionResult Get()
{
    return Ok(new { status = "ok", timestamp = DateTime.UtcNow.ToString("o") });
}
```

Express 等價：你需要把特定路由放在 auth middleware 之前：

```javascript
// Express：health 路由放在 authMiddleware 之前
app.get('/api/health', (req, res) => res.json({ status: 'ok' }));
app.use('/api', authMiddleware);  // 之後的路由才需要認證
```

### 取得目前使用者

本專案透過擴充方法 `User.GetUserId()` 從 JWT token 取得使用者 ID：

```csharp
// backend/TarotApi/Extensions/ClaimsPrincipalExtensions.cs
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(sub);
    }
}
```

在 Controller 中使用：

```csharp
var userId = User.GetUserId();  // User 是 ControllerBase 的屬性，代表目前登入的使用者
```

Express 等價：

```javascript
const userId = req.user.sub;  // 假設 auth middleware 已經把 JWT payload 放到 req.user
```

---

## 6. 本專案四個 Controller 逐一解讀

### HealthController — 最簡單的 Controller

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/[controller]")]  // → /api/health
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]  // 唯一不需要認證的端點
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow.ToString("o")  // ISO 8601 格式
        });
    }
}
```

逐行解讀：
- `[ApiController]` — 啟用 API 行為
- `[Route("api/[controller]")]` — 路由前綴 `/api/health`（HealthController 去掉 Controller = health）
- `ControllerBase` — API controller 的基底類別（不含 View 支援，如果要 MVC 才用 `Controller`）
- `[HttpGet]` — 對應 GET 請求
- `[AllowAnonymous]` — 不需要認證（覆蓋全域 `[Authorize]`）
- `IActionResult` — 不指定回傳型別
- `Ok(...)` — 回傳 200 + JSON body

Express 完整等價：

```javascript
// routes/health.js
const express = require('express');
const router = express.Router();

router.get('/', (req, res) => {
    res.json({
        status: 'ok',
        timestamp: new Date().toISOString()
    });
});

module.exports = router;

// app.js
app.use('/api/health', require('./routes/health'));  // 不套用 auth middleware
```

---

### TarotController — 靜態資料查詢

```csharp
using Microsoft.AspNetCore.Mvc;
using TarotApi.Data;
using TarotApi.Models.Dtos;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/tarot")]  // 直接寫死路由，沒用 [controller] token
public class TarotController : ControllerBase
{
    [HttpGet("cards")]  // → GET /api/tarot/cards
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

    [HttpGet("cards/{id}")]  // → GET /api/tarot/cards/{id}
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
            MeaningUpright = card.MeaningUpright,
            MeaningReversed = card.MeaningReversed,
            Keywords = card.Keywords
        });
    }
}
```

重點觀察：
- **沒有建構子注入**：因為 `TarotCards` 是靜態類別（78 張牌的資料寫死在程式碼裡），不需要依賴注入
- **DTO 轉換**：手動把內部的 Card model 轉成對外的 DTO。`GetAllCards` 回傳精簡版 `TarotCardSummaryDto`，`GetCardById` 回傳詳細版 `TarotCardDetailDto`
- **`card is null`**：C# 的 pattern matching 語法，等同 `card == null` 但更慣用
- **`[HttpGet("cards")]`**：子路徑會加在 Controller 的 `[Route("api/tarot")]` 後面

Express 等價：

```javascript
const router = express.Router();
const TarotCards = require('../data/tarotCards');

// GET /api/tarot/cards
router.get('/cards', (req, res) => {
    const cards = TarotCards.all.map(c => ({
        id: c.id, name: c.name, nameCht: c.nameCht,
        arcana: c.arcana, suit: c.suit, number: c.number
    }));
    res.json(cards);
});

// GET /api/tarot/cards/:id
router.get('/cards/:id', (req, res) => {
    const card = TarotCards.getById(req.params.id);
    if (!card) return res.status(404).json({ error: '找不到該塔羅牌', code: 'NOT_FOUND' });

    res.json({
        id: card.id, name: card.name, nameCht: card.nameCht,
        arcana: card.arcana, suit: card.suit, number: card.number,
        meaningUpright: card.meaningUpright, meaningReversed: card.meaningReversed,
        keywords: card.keywords
    });
});

app.use('/api/tarot', authMiddleware, router);
```

---

### ReadingController — 完整 CRUD

本專案最複雜的 Controller，涵蓋建立、查詢、分頁、統計、每週運勢、刪除：

```csharp
using Microsoft.AspNetCore.Mvc;
using TarotApi.Extensions;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/readings")]
public class ReadingController(ReadingService readingService) : ControllerBase
{
    // POST /api/readings — 建立占卜紀錄
    [HttpPost]
    public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
    {
        var userId = User.GetUserId();
        var result = await readingService.CreateReading(userId, dto.SpreadType, dto.Question);
        return Created($"/api/readings/{result.Id}", result);
    }

    // GET /api/readings?page=1&pageSize=10 — 分頁查詢
    [HttpGet]
    public async Task<ActionResult> GetReadings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50;

        var userId = User.GetUserId();
        var (items, totalCount) = await readingService.GetReadings(userId, page, pageSize);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize
        });
    }

    // GET /api/readings/{id} — 查詢單筆
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReadingResponseDto>> GetReadingById(Guid id)
    {
        var userId = User.GetUserId();
        var result = await readingService.GetReadingById(userId, id);

        if (result is null)
            return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
        return Ok(result);
    }

    // GET /api/readings/stats — 統計資料
    [HttpGet("stats")]
    public async Task<ActionResult<ReadingStatsDto>> GetStats()
    {
        var userId = User.GetUserId();
        var stats = await readingService.GetStats(userId);
        return Ok(stats);
    }

    // GET /api/readings/weekly-fortune — 取得本週運勢
    [HttpGet("weekly-fortune")]
    public async Task<ActionResult> GetWeeklyFortune()
    {
        var userId = User.GetUserId();
        var result = await readingService.GetWeeklyFortune(userId);
        return Ok(new { reading = result, canDraw = result is null });
    }

    // POST /api/readings/weekly-fortune — 建立本週運勢（每週限一次）
    [HttpPost("weekly-fortune")]
    public async Task<ActionResult<ReadingResponseDto>> CreateWeeklyFortune()
    {
        var userId = User.GetUserId();
        try
        {
            var result = await readingService.CreateWeeklyFortune(userId);
            return Created($"/api/readings/{result.Id}", result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto { Error = ex.Message, Code = "WEEKLY_LIMIT" });
        }
    }

    // DELETE /api/readings/{id} — 刪除占卜紀錄
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteReading(Guid id)
    {
        var userId = User.GetUserId();
        var deleted = await readingService.DeleteReading(userId, id);

        if (!deleted)
            return NotFound(new ErrorResponseDto { Error = "找不到該筆占卜紀錄", Code = "NOT_FOUND" });
        return NoContent();
    }
}
```

七個端點一覽：

| 方法 | 路由 | 功能 | 成功狀態碼 | 失敗狀態碼 |
|------|------|------|-----------|-----------|
| `POST` | `/api/readings` | 建立占卜紀錄 | 201 Created | - |
| `GET` | `/api/readings` | 分頁查詢紀錄 | 200 OK | - |
| `GET` | `/api/readings/{id}` | 查詢單筆紀錄 | 200 OK | 404 Not Found |
| `GET` | `/api/readings/stats` | 統計資料 | 200 OK | - |
| `GET` | `/api/readings/weekly-fortune` | 取得本週運勢 | 200 OK | - |
| `POST` | `/api/readings/weekly-fortune` | 建立本週運勢 | 201 Created | 409 Conflict |
| `DELETE` | `/api/readings/{id}` | 刪除紀錄 | 204 No Content | 404 Not Found |

重點觀察：

1. **Primary Constructor 語法**（見下一節詳細解說）：`ReadingController(ReadingService readingService)` 直接在類別宣告時注入依賴

2. **`User.GetUserId()`**：每個需要認證的端點都先取得使用者 ID，確保使用者只能操作自己的資料

3. **分頁參數防禦**：手動限制 `page >= 1`、`1 <= pageSize <= 50`

4. **try-catch 用於商業邏輯錯誤**：`CreateWeeklyFortune` 用 try-catch 攔截 `InvalidOperationException`，轉成 409 Conflict 回應。這是一種常見的模式 — Service 層丟出 exception，Controller 層決定 HTTP 狀態碼

5. **路由衝突避免**：`{id:guid}` 約束確保 `/api/readings/stats` 不會被誤認為 `{id}` 參數（因為 "stats" 不是合法的 GUID）

---

### ProfileController — 基本 CRUD

```csharp
using Microsoft.AspNetCore.Mvc;
using TarotApi.Extensions;
using TarotApi.Models.Dtos;
using TarotApi.Services;

namespace TarotApi.Controllers;

[ApiController]
[Route("api/profile")]
public class ProfileController(ProfileService profileService) : ControllerBase
{
    // GET /api/profile — 取得個人資料
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userId = User.GetUserId();
        var profile = await profileService.GetProfile(userId);

        if (profile is null)
            return NotFound(new ErrorResponseDto { Error = "找不到使用者資料", Code = "NOT_FOUND" });
        return Ok(profile);
    }

    // PUT /api/profile — 更新個人資料
    [HttpPut]
    public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] ProfileUpdateDto dto)
    {
        var userId = User.GetUserId();
        var profile = await profileService.UpdateProfile(userId, dto.DisplayName);

        if (profile is null)
            return NotFound(new ErrorResponseDto { Error = "找不到使用者資料", Code = "NOT_FOUND" });
        return Ok(profile);
    }
}
```

重點觀察：
- 結構跟 ReadingController 類似，但更簡單（只有 GET 和 PUT）
- 同樣使用 Primary Constructor 注入 `ProfileService`
- 注意 `/api/profile` 沒有用 `[controller]` token，因為 "profile" 剛好就是類別名稱

Express 等價：

```javascript
const router = express.Router();

router.get('/', async (req, res) => {
    const userId = req.user.sub;
    const profile = await profileService.getProfile(userId);
    if (!profile) return res.status(404).json({ error: '找不到使用者資料', code: 'NOT_FOUND' });
    res.json(profile);
});

router.put('/', async (req, res) => {
    const userId = req.user.sub;
    const { displayName } = req.body;
    const profile = await profileService.updateProfile(userId, displayName);
    if (!profile) return res.status(404).json({ error: '找不到使用者資料', code: 'NOT_FOUND' });
    res.json(profile);
});

app.use('/api/profile', authMiddleware, router);
```

---

## 7. Primary Constructor 語法

C# 12 引入了 **Primary Constructor**，讓你可以在類別宣告時直接定義建構子參數。本專案的 `ReadingController` 和 `ProfileController` 都使用了這個語法。

### 傳統寫法

```csharp
public class ReadingController : ControllerBase
{
    private readonly ReadingService _readingService;

    public ReadingController(ReadingService readingService)
    {
        _readingService = readingService;
    }

    [HttpPost]
    public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
    {
        var result = await _readingService.CreateReading(...);  // 使用 _readingService
        return Created(...);
    }
}
```

### Primary Constructor 寫法（本專案使用）

```csharp
public class ReadingController(ReadingService readingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
    {
        var result = await readingService.CreateReading(...);  // 直接使用 readingService
        return Created(...);
    }
}
```

差異：
- 不需要宣告 `private readonly` 欄位
- 不需要寫建構子方法
- 參數 `readingService` 在整個類別中都可以使用
- 程式碼更簡潔

### 對 Node.js 開發者來說

這類似 JavaScript class 的建構子簡寫。想像一下如果 JavaScript 支援這種語法：

```javascript
// 假設的 JavaScript 語法（不存在）
class ReadingController(readingService) extends ControllerBase {
    async createReading(dto) {
        return await readingService.createReading(...);  // 直接用
    }
}

// 目前的 JavaScript 寫法
class ReadingController extends ControllerBase {
    #readingService;
    constructor(readingService) {
        super();
        this.#readingService = readingService;
    }
    async createReading(dto) {
        return await this.#readingService.createReading(...);
    }
}
```

### 注意事項

Primary Constructor 的參數是 **可變的**（mutable），不像傳統寫法中 `private readonly` 欄位是唯讀的。在大多數情況下這不是問題（你不會去改寫注入的 service），但值得知道這個差異。

---

## 8. 完整路由總覽

整理本專案所有 API 端點：

| HTTP Method | 路由 | Controller | 認證 | 說明 |
|------------|------|-----------|------|------|
| GET | `/api/health` | HealthController | 不需要 | 健康檢查 |
| GET | `/api/tarot/cards` | TarotController | 需要 | 全部 78 張牌 |
| GET | `/api/tarot/cards/{id}` | TarotController | 需要 | 單張牌詳情 |
| POST | `/api/readings` | ReadingController | 需要 | 建立占卜 |
| GET | `/api/readings` | ReadingController | 需要 | 分頁查詢 |
| GET | `/api/readings/{id}` | ReadingController | 需要 | 單筆查詢 |
| GET | `/api/readings/stats` | ReadingController | 需要 | 統計資料 |
| GET | `/api/readings/weekly-fortune` | ReadingController | 需要 | 本週運勢 |
| POST | `/api/readings/weekly-fortune` | ReadingController | 需要 | 建立週運勢 |
| DELETE | `/api/readings/{id}` | ReadingController | 需要 | 刪除紀錄 |
| GET | `/api/profile` | ProfileController | 需要 | 個人資料 |
| PUT | `/api/profile` | ProfileController | 需要 | 更新資料 |

---

## 9. 重點回顧

| 概念 | Express | ASP.NET Core |
|------|---------|-------------|
| 定義路由 | `router.get('/path', handler)` | `[HttpGet("path")]` 屬性 |
| 路由分組 | `app.use('/prefix', router)` | `[Route("prefix")]` 在 Controller 類別上 |
| 取得 URL 參數 | `req.params.id` | 方法參數 `Guid id` + `{id}` 路由 |
| 取得 Query String | `req.query.page` | `[FromQuery] int page` |
| 取得 Request Body | `req.body` | `[FromBody] MyDto dto` |
| 回傳 JSON | `res.json(data)` | `Ok(data)` |
| 設定狀態碼 | `res.status(404).json(...)` | `NotFound(...)` |
| 全域 Middleware | `app.use(middleware)` | `options.Filters.Add(...)` 在 `AddControllers` |
| 跳過 Middleware | 路由放在 middleware 之前 | `[AllowAnonymous]` 屬性 |
| 依賴注入 | `require()` / `import` | 建構子注入（Primary Constructor） |
| 取得目前使用者 | `req.user` | `User.GetUserId()`（擴充方法） |
