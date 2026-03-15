# 07 — 認證與中介軟體 — 給 Node.js 開發者

> 這份文件以本專案（Tarot App）的實際程式碼為例，說明 ASP.NET Core 如何處理 JWT 認證與自訂中介軟體。如果你熟悉 Express + Passport，會發現概念相通，只是寫法不同。

---

## 1. JWT 認證流程（概念回顧）

JWT（JSON Web Token）在 Node.js 和 .NET 世界中都被廣泛使用，核心流程完全一樣：

```
Client 發送請求
  → Authorization: Bearer <token>
    → Server 驗證 token（簽章、過期時間、issuer、audience）
      → 從 token payload 取出使用者資訊
        → 執行業務邏輯
```

在本專案中：
1. **前端**透過 Supabase Auth 讓使用者登入，取得 JWT（ES256 演算法）
2. **後端**（.NET）透過 JWKS 端點取得公鑰，驗證 token 簽章
3. 驗證通過後，從 token 的 `sub` claim 取出使用者 ID，傳給 Service 層

---

## 2. ASP.NET Core 的 JWT 認證設定

以下是本專案 `Program.cs` 中的 JWT 設定（取自實際程式碼）：

```csharp
// backend/TarotApi/Program.cs

// JWT Authentication — Supabase uses ES256 (asymmetric), so we fetch the public key
// via JWKS. The HS256 JWT secret is not used for signature validation here.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Authority: 告訴 .NET「去這個 URL 取得 OpenID Connect 設定」
        // 類似 Express 中 passport-jwt 的 jwksUri 設定
        options.Authority = $"{supabaseUrl}/auth/v1";

        // MetadataAddress: OpenID Connect Discovery 文件的確切位置
        // .NET 會從這裡找到 JWKS endpoint，自動下載公鑰
        options.MetadataAddress = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";

        // 本地開發時 Supabase 可能是 http，生產環境則強制 https
        options.RequireHttpsMetadata = supabaseUrl.StartsWith("https");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,                          // 檢查 token 的 iss 欄位
            ValidIssuer = $"{supabaseUrl}/auth/v1",         // 必須是 Supabase Auth 發出的

            ValidateAudience = true,                        // 檢查 token 的 aud 欄位
            ValidAudience = "authenticated",                // Supabase 登入使用者的 audience

            ValidateLifetime = true,                        // 檢查 token 是否過期（exp claim）

            ValidateIssuerSigningKey = true,                // 驗證簽章（用 JWKS 取得的公鑰）
        };
    });
builder.Services.AddAuthorization();
```

### 對應的 Express 寫法

如果你在 Node.js 用 `passport-jwt` + `jwks-rsa`，同樣的事情大概長這樣：

```javascript
// Express + passport-jwt + jwks-rsa
const jwksClient = require('jwks-rsa');
const { Strategy: JwtStrategy, ExtractJwt } = require('passport-jwt');

passport.use(new JwtStrategy({
  // 從 Authorization header 取出 Bearer token
  jwtFromRequest: ExtractJwt.fromAuthHeaderAsBearerToken(),

  // 用 JWKS 端點動態取得公鑰（對應 .NET 的 MetadataAddress + Authority）
  secretOrKeyProvider: jwksClient.passportJwtSecret({
    jwksUri: `${supabaseUrl}/auth/v1/.well-known/jwks.json`,
    cache: true,
    rateLimit: true,
  }),

  // 對應 ValidIssuer
  issuer: `${supabaseUrl}/auth/v1`,

  // 對應 ValidAudience
  audience: 'authenticated',

  // passport-jwt 預設就會檢查 exp，對應 ValidateLifetime
}, (jwtPayload, done) => {
  // jwtPayload 就是解碼後的 token 內容
  return done(null, jwtPayload);
}));

// 然後在路由上使用
app.get('/api/readings', passport.authenticate('jwt', { session: false }), handler);
```

### 關鍵差異

| 面向 | Express (passport-jwt) | ASP.NET Core |
|------|----------------------|--------------|
| 安裝方式 | `npm install passport passport-jwt jwks-rsa` | 內建於 `Microsoft.AspNetCore.Authentication.JwtBearer` |
| 設定位置 | 各處分散（strategy, middleware, route） | 集中在 `Program.cs` |
| JWKS 取得 | 手動設定 `jwksUri` | 自動從 `MetadataAddress` 探索 |
| 套用方式 | 每條路由加 `passport.authenticate(...)` | 全域 filter 或 `[Authorize]` attribute |
| Token 取得 | `ExtractJwt.fromAuthHeaderAsBearerToken()` | 框架自動從 `Authorization` header 取得 |

---

## 3. JWKS 驗證（非對稱加密）

### 什麼是 JWKS？

JWKS（JSON Web Key Set）是一組公鑰的 JSON 格式。當 JWT 使用非對稱演算法（如 ES256、RS256）簽署時，驗證方不需要知道私鑰，只需要公鑰。

```
Supabase（擁有私鑰）                    你的 Backend（只有公鑰）
  │                                         │
  │  1. 用私鑰簽署 JWT                       │
  │  ──────────────────►                    │
  │                      User 拿到 JWT       │
  │                        │                 │
  │                        │  2. Bearer token│
  │                        │  ──────────────►│
  │                                          │
  │  3. Backend 去 JWKS endpoint 取公鑰       │
  │  ◄──────────────────────────────────────│
  │  回傳公鑰                                 │
  │  ──────────────────────────────────────►│
  │                                          │
  │                   4. 用公鑰驗證 JWT 簽章    │
```

### 為什麼比對稱加密（HS256）安全？

- **HS256（對稱）**：簽署和驗證都用同一把密鑰。你的 Backend 必須持有 Supabase 的 JWT Secret。如果洩漏，攻擊者可以偽造 token。
- **ES256（非對稱）**：私鑰只在 Supabase，你的 Backend 只拿到公鑰。就算公鑰洩漏，也無法偽造 token。

本專案雖然環境變數中有 `SUPABASE_JWT_SECRET`（程式啟動時會檢查），但實際驗證走的是 JWKS 公鑰：

```csharp
// Program.cs 的註解也說明了這點：
// JWT Authentication — Supabase uses ES256 (asymmetric), so we fetch the public key
// via JWKS. The HS256 JWT secret is not used for signature validation here.
```

### OpenID Connect Discovery

`.well-known/openid-configuration` 是一個標準端點，回傳的 JSON 包含：

```json
{
  "issuer": "https://xxx.supabase.co/auth/v1",
  "jwks_uri": "https://xxx.supabase.co/auth/v1/.well-known/jwks.json",
  "authorization_endpoint": "...",
  "token_endpoint": "..."
}
```

.NET 的 `AddJwtBearer` 設定 `MetadataAddress` 後，會自動：
1. 抓取這個 JSON
2. 從中找到 `jwks_uri`
3. 下載公鑰
4. 快取公鑰（不會每次請求都重新下載）

---

## 4. 全域授權（Global Authorization）

### 預設全部需要認證

本專案用「預設拒絕」策略 — 所有端點都需要認證，除非明確標記例外：

```csharp
// backend/TarotApi/Program.cs

// Controllers with global [Authorize]
builder.Services.AddControllers(options =>
{
    // 加入全域授權 filter：所有 controller action 都要求已認證使用者
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build()));
});
```

這等同於在每一個 Controller 上面加 `[Authorize]`，但只要寫一次。

### 例外：用 `[AllowAnonymous]` 放行

Health check 端點不需要認證（給 Zeabur 做健康檢查用）：

```csharp
// backend/TarotApi/Controllers/HealthController.cs

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]  // ← 覆蓋全域 [Authorize]，允許匿名存取
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "ok",
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
}
```

### Express 對比

在 Express 中，你通常是「預設允許，個別保護」：

```javascript
// Express：每條需要認證的路由個別加 middleware
app.get('/api/readings', requireAuth, getReadings);
app.get('/api/health', getHealth);  // 不加 middleware = 不需認證
```

或者用 router-level middleware 做類似全域保護：

```javascript
// 稍微接近 .NET 的做法
const apiRouter = express.Router();
apiRouter.use(requireAuth);  // 這個 router 底下的路由都要認證
apiRouter.get('/readings', getReadings);

// Health 放在另一個 router，不套認證
app.get('/api/health', getHealth);
app.use('/api', apiRouter);
```

.NET 的 `[AllowAnonymous]` 比 Express 更直覺 — 不需要拆分 router，直接在 action 上標記即可。

---

## 5. ClaimsPrincipal 與 Extension Method

### Claims 是什麼？

JWT 驗證通過後，token payload 裡的資訊會被轉換成 **Claims**（聲明）。一個 Claim 就是一組 key-value：

```
JWT payload:                    .NET Claims:
{                               ┌───────────────────────────────────────┐
  "sub": "a1b2c3...",    →     │ ClaimTypes.NameIdentifier = "a1b2c3..." │
  "email": "user@mail",  →     │ ClaimTypes.Email = "user@mail"         │
  "aud": "authenticated", →     │ "aud" = "authenticated"                │
  "exp": 1700000000       →     │ (用於驗證，不存為 claim)                  │
}                               └───────────────────────────────────────┘
```

在 Controller 中，你可以透過 `User`（型別是 `ClaimsPrincipal`）存取這些資訊。

### 本專案的 Extension Method

本專案用一個 Extension Method 統一取出使用者 ID：

```csharp
// backend/TarotApi/Extensions/ClaimsPrincipalExtensions.cs

using System.Security.Claims;

namespace TarotApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    //  ↓ 注意這個 "this" 關鍵字 — 它讓這個方法可以像實例方法一樣呼叫
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(sub);
    }
}
```

### Extension Method 是什麼？

Extension Method 讓你「替現有的類別新增方法」，而不用修改或繼承它。關鍵是參數列中的 `this` 關鍵字：

```csharp
// 定義：第一個參數加上 this，表示「這個方法可以被 ClaimsPrincipal 呼叫」
public static Guid GetUserId(this ClaimsPrincipal user) { ... }

// 使用：看起來像是 ClaimsPrincipal 本來就有的方法
var userId = User.GetUserId();

// 其實編譯器會轉換成：
var userId = ClaimsPrincipalExtensions.GetUserId(User);
```

在 JavaScript 中，類似的概念是在 prototype 上加方法（但那通常被認為是 bad practice）：

```javascript
// JavaScript 不推薦的做法（但概念類似）：
Object.defineProperty(user, 'getUserId', {
  value() { return this.sub; }
});
```

### Controller 中的使用方式

在 `ReadingController` 中，每個 action 都這樣取得使用者 ID：

```csharp
// backend/TarotApi/Controllers/ReadingController.cs

[ApiController]
[Route("api/readings")]
public class ReadingController(ReadingService readingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReadingResponseDto>> CreateReading([FromBody] ReadingCreateDto dto)
    {
        var userId = User.GetUserId();  // ← 一行搞定，乾淨俐落
        var result = await readingService.CreateReading(userId, dto.SpreadType, dto.Question);
        return Created($"/api/readings/{result.Id}", result);
    }

    [HttpGet]
    public async Task<ActionResult> GetReadings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.GetUserId();  // ← 每個 action 都能用
        var (items, totalCount) = await readingService.GetReadings(userId, page, pageSize);
        return Ok(new { items, totalCount, page, pageSize });
    }

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

### Express 對比

```javascript
// Express + Passport：驗證通過後，user info 在 req.user
app.get('/api/readings', requireAuth, async (req, res) => {
  const userId = req.user.sub;  // 直接取 — 沒有型別安全
  const readings = await readingService.getReadings(userId, page, pageSize);
  res.json(readings);
});
```

| 面向 | Express | ASP.NET Core |
|------|---------|-------------|
| 使用者資訊存在 | `req.user` | `HttpContext.User`（Controller 中簡寫為 `User`） |
| 取得 user ID | `req.user.sub` | `User.GetUserId()`（透過 Extension Method） |
| 型別安全 | 無（除非自訂 TypeScript 型別） | 有（回傳 `Guid`，找不到則拋例外） |

---

## 6. 自訂中介軟體 — ExceptionHandlingMiddleware

### 完整程式碼

```csharp
// backend/TarotApi/Middleware/ExceptionHandlingMiddleware.cs

using System.Text.Json;
using TarotApi.Models.Dtos;

namespace TarotApi.Middleware;

// Primary Constructor：參數直接寫在類別名稱後面（C# 12 語法）
// 等同於寫一個 constructor 並把 next 和 logger 存成欄位
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // InvokeAsync 是中介軟體的進入點（慣例名稱，框架會自動呼叫）
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);  // 呼叫下一個中介軟體（像 Express 的 next()）
        }
        catch (UnauthorizedAccessException ex)
        {
            // GetUserId() 找不到 user 時拋出的例外 → 回傳 401
            logger.LogWarning(ex, "Unauthorized access");
            await WriteErrorResponse(context, StatusCodes.Status401Unauthorized, ex.Message, "UNAUTHORIZED");
        }
        catch (Exception ex)
        {
            // 所有其他未處理的例外 → 回傳 500
            logger.LogError(ex, "Unhandled exception");
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError, "伺服器內部錯誤", "INTERNAL_ERROR");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, string error, string code)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var response = new ErrorResponseDto { Error = error, Code = code };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
```

錯誤回應的 DTO：

```csharp
// backend/TarotApi/Models/Dtos/ErrorResponseDto.cs

public record ErrorResponseDto
{
    public string Error { get; init; } = string.Empty;
    public string? Code { get; init; }
}
```

### 註冊中介軟體

```csharp
// backend/TarotApi/Program.cs — Middleware 段落

app.UseMiddleware<ExceptionHandlingMiddleware>();  // ← 放最外層，包住所有後續中介軟體
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### Express 對比

```javascript
// Express 的 error handling middleware
// 注意：必須有 4 個參數（err, req, res, next），Express 才會認定它是 error handler
app.use((err, req, res, next) => {
  if (err instanceof UnauthorizedError) {
    return res.status(401).json({
      error: err.message,
      code: 'UNAUTHORIZED'
    });
  }
  console.error(err);
  res.status(500).json({
    error: '伺服器內部錯誤',
    code: 'INTERNAL_ERROR'
  });
});
```

### 關鍵差異

| 面向 | Express error middleware | ASP.NET Core 自訂 middleware |
|------|------------------------|---------------------------|
| 放置位置 | **最後面**（在所有路由之後） | **最前面**（包住所有後續中介軟體） |
| 識別方式 | 4 個參數 `(err, req, res, next)` | `try/catch` 包住 `await next(context)` |
| 觸發方式 | `next(err)` 或拋出例外 | 拋出例外（被 `catch` 接住） |
| 錯誤傳遞 | 透過 `next(err)` 鏈式傳遞 | 例外自動往上冒泡到最外層 middleware |

為什麼位置不同？因為架構設計不同：

```
Express（洋蔥模型但 error handler 在尾端）：
  路由A → 路由B → ... → error handler
                  ↑ next(err) 跳到這裡

ASP.NET Core（真正的洋蔥模型）：
  ExceptionMiddleware
    └→ CORS
        └→ Authentication
            └→ Authorization
                └→ Controller
                    ↑ 例外往外冒泡，被最外層 catch
```

---

## 7. ILogger — 日誌系統

### 內建 vs 自選

在 Node.js 中，你需要自己選擇 logging library（winston、pino、bunyan），然後手動設定。在 .NET 中，`ILogger` 是框架內建的，透過 DI 注入：

```csharp
// .NET：透過 DI 自動注入（Primary Constructor 語法）
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger  // ← 框架自動提供
)
{
    // logger.LogWarning(ex, "Unauthorized access");
    // logger.LogError(ex, "Unhandled exception");
    // logger.LogInformation("Something happened");
}
```

```javascript
// Node.js：手動建立
const winston = require('winston');
const logger = winston.createLogger({
  level: 'info',
  format: winston.format.json(),
  transports: [new winston.transports.Console()],
});

// logger.warn('Unauthorized access');
// logger.error('Unhandled exception');
// logger.info('Something happened');
```

### Log Level 對照

| .NET | Node.js (winston/pino) | 用途 |
|------|----------------------|------|
| `LogTrace` | `silly` / `trace` | 最細節的除錯資訊 |
| `LogDebug` | `debug` | 開發時的除錯資訊 |
| `LogInformation` | `info` | 一般運行資訊 |
| `LogWarning` | `warn` | 可能有問題但不影響運行 |
| `LogError` | `error` | 錯誤，需要注意 |
| `LogCritical` | `fatal` | 嚴重錯誤，系統可能無法繼續 |

`ILogger<T>` 中的泛型 `T` 會自動成為 log 的 category name，方便篩選：

```
warn: TarotApi.Middleware.ExceptionHandlingMiddleware[0]
      Unauthorized access
```

---

## 8. 本專案完整認證流程圖

以下是一個請求從前端到後端的完整認證旅程：

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              前端（SvelteKit）                               │
│                                                                             │
│  1. 使用者登入                                                                │
│     └→ Supabase Auth 回傳 JWT（ES256 簽署）                                   │
│                                                                             │
│  2. hooks.server.ts 取得 session                                             │
│     └→ const { session } = await event.locals.safeGetSession();             │
│                                                                             │
│  3. server/api.ts 建立 API client，帶上 Bearer token                         │
│     └→ Authorization: `Bearer ${accessToken}`                               │
│                                                                             │
│  4. 發送請求                                                                  │
│     └→ fetch(`${baseUrl}/api/readings`, { headers })                        │
└─────────────────────────────────────────────────────────┬───────────────────┘
                                                          │
                                               HTTP Request│
                                    Authorization: Bearer eyJ...│
                                                          │
                                                          ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          後端（ASP.NET Core）                                │
│                                                                             │
│  ┌──────────────────────────────────────────────────────────────────────┐   │
│  │ ExceptionHandlingMiddleware                                          │   │
│  │   try {                                                              │   │
│  │   ┌──────────────────────────────────────────────────────────────┐   │   │
│  │   │ CORS Middleware                                              │   │   │
│  │   │   ✓ 檢查 Origin 是否在 ALLOWED_ORIGINS 中                     │   │   │
│  │   │   ┌──────────────────────────────────────────────────────┐   │   │   │
│  │   │   │ Authentication Middleware                            │   │   │   │
│  │   │   │   ✓ 從 header 取出 Bearer token                      │   │   │   │
│  │   │   │   ✓ 去 JWKS endpoint 取得公鑰（有快取）                │   │   │   │
│  │   │   │   ✓ 驗證簽章（ES256）                                 │   │   │   │
│  │   │   │   ✓ 驗證 issuer, audience, lifetime                  │   │   │   │
│  │   │   │   ✓ 將 payload 轉為 ClaimsPrincipal → HttpContext.User│   │   │   │
│  │   │   │   ┌──────────────────────────────────────────────┐   │   │   │   │
│  │   │   │   │ Authorization Middleware                     │   │   │   │   │
│  │   │   │   │   ✓ 全域 filter：RequireAuthenticatedUser    │   │   │   │   │
│  │   │   │   │   ✗ [AllowAnonymous] 則跳過                  │   │   │   │   │
│  │   │   │   │   ┌──────────────────────────────────────┐   │   │   │   │   │
│  │   │   │   │   │ Controller                          │   │   │   │   │   │
│  │   │   │   │   │   var userId = User.GetUserId();    │   │   │   │   │   │
│  │   │   │   │   │   → ClaimsPrincipal                 │   │   │   │   │   │
│  │   │   │   │   │     → FindFirstValue(NameIdentifier)│   │   │   │   │   │
│  │   │   │   │   │       → Guid("a1b2c3...")           │   │   │   │   │   │
│  │   │   │   │   │   → readingService.GetReadings(     │   │   │   │   │   │
│  │   │   │   │   │         userId, page, pageSize)     │   │   │   │   │   │
│  │   │   │   │   │   → return Ok(result)               │   │   │   │   │   │
│  │   │   │   │   └──────────────────────────────────────┘   │   │   │   │   │
│  │   │   │   └──────────────────────────────────────────────┘   │   │   │   │
│  │   │   └──────────────────────────────────────────────────────┘   │   │   │
│  │   └──────────────────────────────────────────────────────────────┘   │   │
│  │   } catch (Exception ex) { → WriteErrorResponse(500, ...) }          │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 中介軟體順序很重要

```csharp
// backend/TarotApi/Program.cs

app.UseMiddleware<ExceptionHandlingMiddleware>();  // 1. 最外層：攔截所有例外
app.UseCors("AllowFrontend");                      // 2. CORS：處理跨域
app.UseAuthentication();                           // 3. 認證：驗證 JWT，建立 User
app.UseAuthorization();                            // 4. 授權：檢查 [Authorize]
app.MapControllers();                              // 5. 路由：進入 Controller
```

順序不能亂 — `UseAuthentication()` 必須在 `UseAuthorization()` 之前（先知道「你是誰」才能判斷「你能不能存取」）。`ExceptionHandlingMiddleware` 放最前面，才能 catch 住所有後續中介軟體拋出的例外。

在 Express 中等價的順序：

```javascript
app.use(cors(corsOptions));                    // 1. CORS
app.use(passport.initialize());                // 2. 初始化認證
app.use('/api/protected', passport.authenticate('jwt'));  // 3. 驗證 + 授權
app.use('/api/protected', routes);             // 4. 路由
app.use(errorHandler);                         // 5. 錯誤處理（放最後！）
```

---

## 重點回顧

| 概念 | Node.js / Express | ASP.NET Core |
|------|-------------------|-------------|
| JWT 驗證 | `passport-jwt` + `jwks-rsa` | 內建 `AddJwtBearer` |
| 全域認證 | Router-level middleware | `AuthorizeFilter` 全域 filter |
| 跳過認證 | 不套 middleware | `[AllowAnonymous]` attribute |
| 取得使用者 | `req.user.sub` | `User.GetUserId()`（Extension Method） |
| 錯誤處理 | Error middleware 放最後 `(err, req, res, next)` | 自訂 middleware 放最前 `try/catch` |
| 日誌 | 自選（winston / pino） | 內建 `ILogger<T>` + DI |
| 中介軟體概念 | `app.use(fn)` | `app.UseMiddleware<T>()` |
