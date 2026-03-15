# ASP.NET Core 基礎 — 給 Node.js 開發者

> 這份文件以 Tarot App 專案的實際程式碼為範例，幫助有 Node.js 後端經驗的開發者快速掌握 ASP.NET Core 的核心觀念。

---

## 1. ASP.NET Core 是什麼

ASP.NET Core 是微軟推出的跨平台 Web 框架，用來建構 Web API 和 Web 應用程式。如果你用過 Express.js，可以把它想成「電池全部內建」的 Express — 不需要額外安裝路由、body parsing、CORS、認證等套件，框架本身就包含了這些功能。

### 和 Express.js 的關鍵差異

| 特性 | Express.js (Node.js) | ASP.NET Core (C#) |
|------|----------------------|-------------------|
| 型別系統 | 動態型別（可搭配 TypeScript） | 靜態強型別 |
| 依賴注入 | 手動 require/import | 內建 DI 容器 |
| 認證/授權 | 需要 passport.js 等套件 | 內建 JWT/Cookie/OAuth |
| Web 伺服器 | 通常搭配 http 模組或 Express 本身 | 內建 Kestrel 高效能伺服器 |
| 路由 | `router.get('/path', handler)` | Attribute-based `[HttpGet("path")]` |
| Body Parsing | 需要 `express.json()` 中介軟體 | 內建，自動反序列化 |
| CORS | 需要 `cors` 套件 | 內建 `AddCors()` |

一句話總結：Express 是「自己組裝」，ASP.NET Core 是「開箱即用」。

---

## 2. Program.cs — 應用程式進入點

在 .NET 8 中採用「Minimal Hosting Model」，不再需要 `Startup.cs`，整個應用的設定集中在 `Program.cs` 一個檔案裡。

### 兩個階段

整個 `Program.cs` 分成明確的兩個階段：

1. **Phase 1 — 註冊服務（Services）**：告訴框架「我的應用需要哪些元件」
2. **Phase 2 — 設定中介軟體管線（Middleware Pipeline）**：決定「HTTP 請求要經過哪些處理步驟」

### Express vs ASP.NET Core 對照

```javascript
// Express (Node.js) — app.js
const express = require('express');
const cors = require('cors');

const app = express();

// 中介軟體
app.use(cors({ origin: allowedOrigins }));
app.use(express.json());
app.use(authMiddleware);

// 路由
app.use('/api/health', healthRouter);
app.use('/api/readings', readingsRouter);
app.use('/api/tarot', tarotRouter);

app.listen(3000);
```

```csharp
// ASP.NET Core (C#) — Program.cs（來自本專案）
var builder = WebApplication.CreateBuilder(args);

// ── Phase 1: 註冊服務 ─────────────────────────────────

// 註冊 Controllers，並設定全域認證過濾器
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AuthorizeFilter(/* ... */));
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

// 註冊應用服務（DI 容器）
builder.Services.AddSingleton<TarotService>();   // 無狀態的牌卡邏輯
builder.Services.AddScoped<ReadingService>();     // 需要 DB，每次請求一個實例
builder.Services.AddScoped<ProfileService>();     // 同上

// JWT 認證
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* Supabase JWKS 設定 */ });

// EF Core + PostgreSQL
builder.Services.AddDbContext<TarotDbContext>(options =>
    options.UseNpgsql(dbConnectionString));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Phase 2: 設定中介軟體管線 ────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();   // 將 Controller 的路由對應到管線

app.Run();
```

### 對照重點

| Express | ASP.NET Core | 說明 |
|---------|--------------|------|
| `const app = express()` | `var builder = WebApplication.CreateBuilder(args)` | 建立應用 |
| `app.use(cors(...))` | `builder.Services.AddCors(...)` + `app.UseCors(...)` | CORS 分成「註冊」和「啟用」兩步 |
| `app.use(express.json())` | 內建，不需要額外設定 | 自動解析 JSON body |
| `app.use('/api/health', router)` | `app.MapControllers()` | 路由由 Controller 的 Attribute 定義 |
| `app.listen(3000)` | `app.Run()` | 啟動伺服器 |

---

## 3. 中介軟體 Pipeline（Middleware）

中介軟體的概念和 Express 完全相同：HTTP 請求進來後，依序經過一連串的處理函式，每個函式可以選擇「傳遞給下一個」或「直接回應」（short-circuit）。

### 順序很重要

和 Express 一樣，**中介軟體的註冊順序就是執行順序**。本專案的管線如下：

```
HTTP Request
    │
    ▼
┌─────────────────────────────────┐
│ 1. ExceptionHandlingMiddleware  │  ← 最外層的 try/catch，捕獲所有未處理例外
├─────────────────────────────────┤
│ 2. CORS                        │  ← 處理跨域請求的預檢和回應標頭
├─────────────────────────────────┤
│ 3. Authentication               │  ← 驗證 JWT token，建立使用者身份
├─────────────────────────────────┤
│ 4. Authorization                │  ← 檢查 [Authorize] 屬性，未認證則回 401
├─────────────────────────────────┤
│ 5. MapControllers               │  ← 匹配路由，執行對應的 Controller Action
└─────────────────────────────────┘
    │
    ▼
HTTP Response
```

### Express vs ASP.NET Core 中介軟體寫法

Express 中介軟體：

```javascript
// Express — 自訂錯誤處理中介軟體
function errorHandler(err, req, res, next) {
    console.error(err);
    res.status(500).json({ error: '伺服器內部錯誤' });
}

app.use(errorHandler);
```

ASP.NET Core 中介軟體（本專案的 `ExceptionHandlingMiddleware`）：

```csharp
// ASP.NET Core — 自訂中介軟體類別
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);  // 傳給下一個中介軟體
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access");
            await WriteErrorResponse(context, 401, ex.Message, "UNAUTHORIZED");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, 500, "伺服器內部錯誤", "INTERNAL_ERROR");
        }
    }
}
```

關鍵差異：
- Express 中介軟體是 `(req, res, next)` 函式；ASP.NET Core 是一個**類別**，透過 `RequestDelegate next` 呼叫下一層
- Express 的錯誤處理中介軟體有 4 個參數 `(err, req, res, next)`；ASP.NET Core 用 `try/catch` 包住 `next(context)` 來攔截例外
- ASP.NET Core 中介軟體透過 DI 注入 `ILogger` 等服務，不需要額外 require

---

## 4. 依賴注入（Dependency Injection）

**這是從 Node.js 轉到 C# 最大的觀念差異。**

### 什麼是 DI？

在 Node.js 中，你通常直接 import 模組來使用：

```javascript
// Node.js — 直接 import
const readingService = require('./services/readingService');
const db = require('./db');

router.post('/', async (req, res) => {
    const result = await readingService.createReading(db, req.userId, req.body);
    res.status(201).json(result);
});
```

在 C# 中，你不會直接 new 一個服務或直接 import 來用。取而代之的是：

1. 在 `Program.cs` 中**註冊**服務到 DI 容器
2. 在需要使用的地方，透過**建構子參數**宣告「我需要這個服務」
3. 框架會**自動**建立並注入正確的實例

```csharp
// Program.cs — 步驟 1：註冊
builder.Services.AddScoped<ReadingService>();

// ReadingController.cs — 步驟 2：宣告需要
public class ReadingController(ReadingService readingService) : ControllerBase
{
    // readingService 由框架自動注入，直接使用即可
}
```

**好處**：程式碼更鬆耦合、更容易寫單元測試（可以注入 mock 物件）。

### 三種生命週期

註冊服務時，你必須選擇它的**生命週期**（lifetime），決定實例何時建立、何時銷毀：

#### 1. Singleton — 全域唯一實例

```csharp
builder.Services.AddSingleton<TarotService>();
```

- 應用啟動時建立一個實例，所有請求共用同一個
- **Node.js 類比**：模組層級的常數。當你 `require('./tarotService')` 時，Node.js 會快取模組，之後每次 require 都拿到同一個物件

```javascript
// Node.js 的「Singleton」
// tarotService.js
const cards = loadAllCards();  // 只執行一次
module.exports = { getCard, shuffle };  // 所有 require 都共用同一份
```

- **本專案**：`TarotService` 只負責牌卡資料和洗牌邏輯，不碰資料庫、不存狀態，因此用 Singleton 安全又高效

#### 2. Scoped — 每次 HTTP 請求一個實例

```csharp
builder.Services.AddScoped<ReadingService>();
builder.Services.AddScoped<ProfileService>();
```

- 同一個 HTTP 請求內，所有注入的地方拿到同一個實例
- 請求結束後自動銷毀（Dispose）
- **Node.js 類比**：在 Express middleware 中為每個 request 建立一個 context 物件

```javascript
// Node.js 的「Scoped」概念
app.use((req, res, next) => {
    req.dbConnection = db.getConnection();  // 每個 request 一個連線
    next();
});
```

- **本專案**：`ReadingService` 和 `ProfileService` 需要 `TarotDbContext`（EF Core 的資料庫連線），資料庫連線本身就是 Scoped 的，所以依賴它的服務也必須是 Scoped

#### 3. Transient — 每次注入都建立新實例

```csharp
builder.Services.AddTransient<SomeService>();
```

- 每次有人需要這個服務，就 new 一個全新的實例
- **Node.js 類比**：每次呼叫工廠函式

```javascript
// Node.js 的「Transient」概念
function createParser() {
    return new XmlParser();  // 每次都是新的
}
```

- **本專案沒有使用 Transient**，但它適合用在無狀態且輕量的工具類服務

#### 生命週期總覽

| 生命週期 | 建立時機 | 銷毀時機 | Node.js 類比 | 專案範例 |
|----------|----------|----------|--------------|----------|
| Singleton | 應用啟動（第一次使用時） | 應用關閉 | `module.exports` 快取 | `TarotService` |
| Scoped | 每個 HTTP Request | Request 結束 | `req.context = {}` | `ReadingService`, `ProfileService` |
| Transient | 每次注入 | GC 回收 | `new SomeClass()` | （本專案未使用） |

### 建構子注入（Constructor Injection）

這是 DI 最常見的用法 — 透過建構子參數宣告依賴：

```csharp
// C# — 本專案的 ReadingController
// 使用 C# 12 的 Primary Constructor 語法
[ApiController]
[Route("api/readings")]
public class ReadingController(ReadingService readingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReadingResponseDto>> CreateReading(
        [FromBody] ReadingCreateDto dto)
    {
        var userId = User.GetUserId();
        var result = await readingService.CreateReading(userId, dto.SpreadType, dto.Question);
        return Created($"/api/readings/{result.Id}", result);
    }
}
```

對比 Express 的寫法：

```javascript
// Express — 手動引入依賴
const readingService = require('../services/readingService');

router.post('/', async (req, res) => {
    const result = await readingService.createReading(
        req.userId,
        req.body.spreadType,
        req.body.question
    );
    res.status(201).json(result);
});
```

在 C# 中，你完全不需要 `require` 或 `import` 來取得 `readingService` — 只要在建構子裡宣告型別，框架就會自動把正確的實例送進來。

---

## 5. 路由系統 — Attribute Routing

Express 使用函式呼叫來定義路由，ASP.NET Core 使用**屬性（Attribute）**標註在類別和方法上。

### 基本對照

```javascript
// Express
router.get('/api/tarot/cards', getAllCards);
router.get('/api/tarot/cards/:id', getCardById);
router.post('/api/readings', createReading);
router.delete('/api/readings/:id', deleteReading);
```

```csharp
// ASP.NET Core — TarotController
[ApiController]
[Route("api/tarot")]                          // 基底路徑
public class TarotController : ControllerBase
{
    [HttpGet("cards")]                         // GET /api/tarot/cards
    public ActionResult<List<TarotCardSummaryDto>> GetAllCards() { ... }

    [HttpGet("cards/{id}")]                    // GET /api/tarot/cards/{id}
    public ActionResult<TarotCardDetailDto> GetCardById(string id) { ... }
}

// ASP.NET Core — ReadingController
[ApiController]
[Route("api/readings")]                        // 基底路徑
public class ReadingController(ReadingService readingService) : ControllerBase
{
    [HttpPost]                                 // POST /api/readings
    public async Task<ActionResult<ReadingResponseDto>> CreateReading(...) { ... }

    [HttpGet("{id:guid}")]                     // GET /api/readings/{some-guid}
    public async Task<ActionResult<ReadingResponseDto>> GetReadingById(Guid id) { ... }

    [HttpDelete("{id:guid}")]                  // DELETE /api/readings/{some-guid}
    public async Task<ActionResult> DeleteReading(Guid id) { ... }
}
```

### 路由重點

| 概念 | Express | ASP.NET Core |
|------|---------|-------------|
| 基底路徑 | `app.use('/api/readings', router)` | `[Route("api/readings")]` 在 class 上 |
| HTTP 方法 | `router.get()`, `router.post()` | `[HttpGet]`, `[HttpPost]` |
| 路徑參數 | `:id` | `{id}` |
| 型別約束 | 無（自己驗證） | `{id:guid}`, `{id:int}` 等 |
| Query 參數 | `req.query.page` | `[FromQuery] int page` |
| Request Body | `req.body` | `[FromBody] ReadingCreateDto dto` |
| Auto-naming | 無 | `[Route("api/[controller]")]` 自動取類別名 |

`[Route("api/[controller]")]` 是一個有趣的語法糖 — `[controller]` 會自動替換成類別名稱去掉 `Controller` 後綴。例如本專案的 `HealthController` 使用了這個寫法：

```csharp
[Route("api/[controller]")]   // → api/health
public class HealthController : ControllerBase { ... }
```

### 特殊屬性

```csharp
[AllowAnonymous]              // 跳過認證，類似 Express 中不加 authMiddleware
[ApiController]               // 啟用自動 Model Validation、自動 400 回應等
```

本專案在 `Program.cs` 設定了全域 `[Authorize]`，所以所有 Controller 預設都需要認證。只有 `HealthController` 的 `Get()` 方法標註了 `[AllowAnonymous]` 來豁免。

---

## 6. 環境管理

### Node.js vs ASP.NET Core

| | Node.js | ASP.NET Core |
|---|---------|-------------|
| 環境變數 | `NODE_ENV` | `ASPNETCORE_ENVIRONMENT` |
| 判斷方式 | `process.env.NODE_ENV === 'production'` | `app.Environment.IsDevelopment()` |
| 預設值 | 無（通常自己設） | `Development` |
| 常見環境 | development, production | Development, Staging, Production |

### 本專案的使用方式

```csharp
// Program.cs — 只在開發環境啟用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```

這就像 Express 中：

```javascript
if (process.env.NODE_ENV !== 'production') {
    app.use('/swagger', swaggerUi.serve, swaggerUi.setup(swaggerDocument));
}
```

所以本專案在線上環境（`rtarot-api.zeabur.app`）不會暴露 Swagger UI，只有本地開發時可以透過 `/swagger` 存取 API 文件。

### 環境變數讀取

```csharp
// C# — 直接用 Environment.GetEnvironmentVariable
var jwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")
    ?? throw new InvalidOperationException("SUPABASE_JWT_SECRET is not set");
```

```javascript
// Node.js
const jwtSecret = process.env.SUPABASE_JWT_SECRET;
if (!jwtSecret) throw new Error('SUPABASE_JWT_SECRET is not set');
```

C# 中 `??` 運算子等同於 JavaScript 的 `??`，而 `throw` 可以作為表達式使用（C# 7+），讓這段程式碼非常簡潔。

---

## 7. JSON 序列化

### 內建 System.Text.Json

ASP.NET Core 內建 `System.Text.Json`，功能等同於 JavaScript 的 `JSON.stringify()` / `JSON.parse()`，但提供更多設定選項。

### 本專案的設定

```csharp
// Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
```

這兩行設定做了什麼？

#### CamelCase 命名策略

C# 慣例使用 PascalCase（首字母大寫），但前端 API 慣例是 camelCase（首字母小寫）：

```csharp
// C# Model（PascalCase）
public class ReadingResponseDto
{
    public Guid Id { get; set; }
    public string SpreadType { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

設定 `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` 後，API 回應自動轉為：

```json
{
    "id": "...",
    "spreadType": "...",
    "createdAt": "..."
}
```

如果不設定，前端會收到 `Id`, `SpreadType`, `CreatedAt` — 不符合 JavaScript 慣例。

#### Enum 字串轉換

```csharp
// 不加 JsonStringEnumConverter：
{ "spreadType": 0 }      // 前端拿到數字，看不懂

// 加了 JsonStringEnumConverter：
{ "spreadType": "single" } // 前端拿到可讀的字串
```

### 對比 Node.js

在 Node.js 中你幾乎不需要擔心這些，因為 JavaScript 物件本身就是 camelCase，`JSON.stringify()` 直接輸出：

```javascript
// Node.js — 天生 camelCase，不需要轉換
const reading = {
    id: '...',
    spreadType: 'single',
    createdAt: new Date()
};
res.json(reading);  // 直接就是 camelCase
```

這是 C# 開發者需要額外處理但 Node.js 開發者可能不會意識到的差異。

---

## 重點回顧

| 觀念 | Node.js 開發者已知 | ASP.NET Core 對應 |
|------|-------------------|-------------------|
| 應用進入點 | `app.js` / `server.js` | `Program.cs` |
| 中介軟體 | `app.use(fn)` | `app.UseMiddleware<T>()` |
| 路由 | `router.get('/path', handler)` | `[HttpGet("path")]` Attribute |
| 依賴管理 | `require()` / `import` | DI 容器 + 建構子注入 |
| 環境判斷 | `NODE_ENV` | `ASPNETCORE_ENVIRONMENT` |
| JSON 處理 | `JSON.stringify/parse` | `System.Text.Json`（需設定 camelCase） |
| 錯誤處理 | Error middleware `(err, req, res, next)` | `try/catch` 中介軟體包住 `next(context)` |

最大的心態轉換：在 Node.js 中你習慣「自己組裝一切」，在 ASP.NET Core 中你需要習慣「向框架註冊，讓框架幫你處理」。DI 是這個模式的核心 — 一旦理解了 DI，其他觀念都會水到渠成。
