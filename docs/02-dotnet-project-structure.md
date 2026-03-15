# .NET 專案結構 — 給 Node.js 開發者

> 這份文件用 Node.js 開發者熟悉的概念，來解釋 .NET 專案的結構與工具鏈。所有範例都來自本專案（Tarot App）的實際程式碼。

---

## 1. .csproj vs package.json

在 Node.js 世界裡，`package.json` 是專案的核心設定檔，記錄了名稱、版本、腳本指令和依賴套件。在 .NET 裡，對應的角色是 `.csproj`（C# Project 檔案），格式是 XML。

### 本專案的 .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>TarotApi</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>

</Project>
```

### 逐段對照解釋

| .csproj 設定 | 對應的 Node.js 概念 | 說明 |
|---|---|---|
| `Sdk="Microsoft.NET.Sdk.Web"` | `express` 或框架樣板 | 告訴 .NET 這是一個 Web 專案，自動引入 ASP.NET Core 所需的建置邏輯 |
| `<TargetFramework>net8.0</TargetFramework>` | `"engines": { "node": ">=22" }` | 指定要用哪個版本的 .NET Runtime，就像你在 `package.json` 指定 Node.js 版本 |
| `<Nullable>enable</Nullable>` | TypeScript 的 `strict: true` | 啟用可空參考型別檢查。編譯器會警告你可能的 `null` 問題，就像 TypeScript 的嚴格模式會抓 `undefined` |
| `<ImplicitUsings>enable</ImplicitUsings>` | 自動 import 常用模組 | .NET 會自動引入常用的 namespace（如 `System`、`System.Linq`），你不用在每個檔案最上面寫一堆 `using`。想像成 Node.js 自動幫你 `import` 了 `path`、`fs` 等常用模組 |
| `<RootNamespace>TarotApi</RootNamespace>` | `"name": "tarot-api"` | 專案的根命名空間，所有類別預設都在這個 namespace 下 |
| `<PackageReference ... />` | `"dependencies": { ... }` | 第三方套件依賴，等同於 `package.json` 的 `dependencies` 區段 |

### 並排比較

```
// package.json                        // TarotApi.csproj
{                                      <Project Sdk="Microsoft.NET.Sdk.Web">
  "name": "tarot-api",                   <PropertyGroup>
  "engines": { "node": ">=22" },           <TargetFramework>net8.0</TargetFramework>
  // (TypeScript strict mode)              <Nullable>enable</Nullable>
  // (auto imports)                        <ImplicitUsings>enable</ImplicitUsings>
                                           <RootNamespace>TarotApi</RootNamespace>
                                         </PropertyGroup>
  "dependencies": {                      <ItemGroup>
    "passport-jwt": "^4.0.0",             <PackageReference Include="...JwtBearer" Version="8.0.11" />
    "prisma": "^5.0.0",                   <PackageReference Include="...PostgreSQL" Version="8.0.11" />
    "swagger-ui-express": "^5.0.0"        <PackageReference Include="...Swashbuckle" Version="6.5.0" />
  }                                      </ItemGroup>
}                                      </Project>
```

> **關鍵差異**：`.csproj` 不需要 `scripts` 區段，因為 .NET CLI 本身就提供了 `dotnet run`、`dotnet build` 等標準指令，不像 Node.js 需要在 `package.json` 裡自己定義 `"start": "node index.js"`。

---

## 2. NuGet vs npm

NuGet 是 .NET 生態系的套件管理器，角色等同於 npm。但有幾個重要的差異：

### 核心差異

| 特性 | npm | NuGet |
|---|---|---|
| 套件倉庫 | npmjs.com | nuget.org |
| 套件安裝位置 | 專案內的 `node_modules/` | 全域快取（`~/.nuget/packages/`） |
| 鎖定檔 | `package-lock.json` | 通常不需要（版本已寫在 `.csproj`） |
| 安裝時機 | 手動執行 `npm install` | `dotnet build` 時自動還原 |
| 套件大小影響 | 每個專案各存一份 | 全域共用，不重複下載 |

### 指令對照

| 操作 | npm | NuGet (.NET CLI) |
|---|---|---|
| 安裝所有依賴 | `npm install` | `dotnet restore` |
| 安裝特定套件 | `npm install express` | `dotnet add package Swashbuckle.AspNetCore` |
| 安裝指定版本 | `npm install express@4.18.0` | `dotnet add package Swashbuckle.AspNetCore --version 6.5.0` |
| 移除套件 | `npm uninstall express` | `dotnet remove package Swashbuckle.AspNetCore` |
| 列出已安裝套件 | `npm list` | `dotnet list package` |
| 查看過期套件 | `npm outdated` | `dotnet list package --outdated` |

### 沒有 node_modules！

這是最讓 Node.js 開發者驚喜的地方。NuGet 套件存在全域快取裡（通常是 `~/.nuget/packages/`），所有專案共用。不會再出現一個專案動輒幾百 MB 的 `node_modules` 資料夾。

執行 `dotnet build` 時，.NET 會自動執行 `dotnet restore`（還原套件），所以你通常不需要手動跑 `dotnet restore`，就像 `npm install` 在大多數工具鏈裡也會被自動觸發一樣。

---

## 3. .NET CLI 常用指令

.NET CLI（`dotnet` 指令）是 .NET 開發的核心工具，就像 `npm` / `npx` 之於 Node.js。

| .NET CLI | npm / Node.js 等價指令 | 說明 |
|---|---|---|
| `dotnet new webapi` | `npm init` + `express-generator` | 建立新的 Web API 專案，自帶樣板程式碼 |
| `dotnet run` | `node index.js` / `npm start` | 編譯並執行專案 |
| `dotnet watch run` | `nodemon` / `tsx watch` | 監聽檔案變更，自動重啟（本專案開發時用這個） |
| `dotnet build` | `tsc` / `npm run build` | 編譯專案但不執行（檢查語法錯誤） |
| `dotnet add package X` | `npm install X` | 安裝第三方套件 |
| `dotnet restore` | `npm install`（只裝依賴） | 還原所有套件依賴 |
| `dotnet test` | `npm test` / `jest` | 執行單元測試 |
| `dotnet publish` | `npm run build`（正式環境） | 產生可部署的正式版本 |
| `dotnet ef migrations add` | `prisma migrate dev` | 建立資料庫遷移 |
| `dotnet ef database update` | `prisma migrate deploy` | 套用資料庫遷移 |

### 本專案的開發指令

```bash
# 進入 backend 資料夾
cd backend

# 開發模式（會監聽檔案變更自動重啟）
dotnet watch run --project TarotApi

# 只編譯不執行（檢查有沒有語法錯誤）
dotnet build TarotApi

# 安裝新套件（例如要加一個 Redis 快取）
dotnet add TarotApi package StackExchange.Redis
```

> **小提示**：`dotnet watch run` 等同於 Node.js 的 `nodemon`，是開發時的好夥伴。本專案的 `docker-compose.yml` 裡就是用 `dotnet watch run` 來啟動後端服務，確保改程式碼後自動重新編譯。

---

## 4. 專案結構慣例

.NET 的專案結構是「慣例優於設定」（convention over configuration）的風格。雖然你可以把所有檔案丟在根目錄，但社群有一套廣泛接受的資料夾命名慣例。

### 本專案的結構

```
TarotApi/
├── Controllers/          # 路由處理器（像 Express 的 Router）
│   ├── HealthController.cs
│   ├── TarotController.cs
│   ├── ReadingController.cs
│   └── ProfileController.cs
├── Services/             # 商業邏輯（像 Node.js 的 service 模組）
│   ├── TarotService.cs
│   ├── ReadingService.cs
│   └── ProfileService.cs
├── Models/               # 資料模型（像 TypeScript 的 interface / type）
│   ├── Dtos/             # Data Transfer Objects（API 回傳的資料格式）
│   ├── Profile.cs
│   ├── Reading.cs
│   ├── SpreadPosition.cs
│   └── SpreadType.cs
├── Data/                 # 資料庫相關（Context、種子資料）
│   ├── TarotDbContext.cs
│   └── TarotCards.cs
├── Middleware/            # 自訂中介軟體（像 Express middleware）
│   └── ExceptionHandlingMiddleware.cs
├── Extensions/           # 擴充方法（像工具函式 helpers）
│   └── ClaimsPrincipalExtensions.cs
├── Properties/           # 啟動設定
│   └── launchSettings.json
├── Program.cs            # 進入點（像 app.js / index.ts）
├── TarotApi.csproj       # 專案檔（像 package.json）
├── appsettings.json      # 設定檔（像 .env 或 config.json）
└── appsettings.Development.json  # 開發環境設定
```

### 與 Express.js 專案的對照

| .NET 資料夾 / 檔案 | Express.js 對應 | 負責什麼 |
|---|---|---|
| `Controllers/` | `routes/` | 定義 API 路由和 HTTP 方法（GET、POST 等） |
| `Services/` | `services/` 或 `lib/` | 商業邏輯，Controller 呼叫 Service 來處理實際工作 |
| `Models/` | TypeScript 的 `types/` 或 `interfaces/` | 定義資料的「長相」——Entity（資料庫對應）和 DTO（API 傳輸用） |
| `Models/Dtos/` | API response 的 type 定義 | 專門定義 API 回傳給前端的資料形狀，與資料庫 Entity 分開 |
| `Data/` | `db/` 或 `prisma/` | 資料庫設定、ORM Context、種子資料 |
| `Middleware/` | Express middleware 函式 | 請求管線中的攔截器（錯誤處理、日誌等） |
| `Extensions/` | `utils/` 或 `helpers/` | 工具函式，本專案用來從 JWT 取出使用者 ID |
| `Program.cs` | `app.js` / `index.ts` | 應用程式的進入點：設定 DI、中介軟體、路由、啟動伺服器 |
| `appsettings.json` | `.env` / `config.json` | 應用程式設定值 |

> **關鍵觀念**：在 Express 裡，你可能把路由、邏輯、資料存取全寫在同一個 route handler 裡。.NET 的慣例是嚴格分層：**Controller 只負責接收請求和回傳回應**，邏輯交給 **Service**，資料存取交給 **Data/DbContext**。這種分層在 Node.js 裡也是好的實踐，只是在 .NET 裡更被強制執行。

---

## 5. appsettings.json vs .env

### 基本概念

在 Node.js 裡，大家習慣用 `.env` 檔案搭配 `dotenv` 套件來管理設定值。.NET 內建了一套更結構化的設定系統。

| 特性 | Node.js (.env) | .NET (appsettings.json) |
|---|---|---|
| 格式 | `KEY=value`（純文字） | JSON（支援巢狀結構） |
| 環境分層 | `.env.development`、`.env.production` | `appsettings.Development.json`、`appsettings.Production.json` |
| 讀取方式 | `process.env.KEY` | `Environment.GetEnvironmentVariable("KEY")` 或 `IConfiguration` |
| 機密管理 | `.env` 不進版控 | User Secrets（開發）或環境變數（正式） |

### 讀取環境變數的對照

```javascript
// Node.js
const supabaseUrl = process.env.PUBLIC_SUPABASE_URL;
const port = process.env.PORT || 3000;
```

```csharp
// C# (.NET)
var supabaseUrl = Environment.GetEnvironmentVariable("PUBLIC_SUPABASE_URL");
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
```

### 本專案的做法

本專案選擇**使用環境變數**而非 `appsettings.json` 來管理機密設定（Supabase URL、JWT Secret、資料庫連線字串）。原因是：

1. **Supabase 整合**：敏感資訊不應寫在設定檔裡提交到版控
2. **Zeabur 部署**：Zeabur 透過環境變數注入設定值，跟 Vercel / Railway 的做法一樣
3. **Docker 開發**：`docker-compose.yml` 透過 `env_file` 載入 `.env`

所以你會在 `Program.cs` 裡看到大量的 `Environment.GetEnvironmentVariable()`，而 `appsettings.json` 只保留了框架層級的基本設定（如日誌等級）。

---

## 6. launchSettings.json

`launchSettings.json` 位於 `Properties/` 資料夾內，是 .NET 專屬的**本地開發設定檔**。

### 它做什麼？

- 設定本地開發時的 HTTP/HTTPS 埠號
- 設定環境名稱（Development / Production）
- 設定是否自動開啟瀏覽器
- 設定環境變數（僅限本地開發）

### Node.js 裡的對應概念

Node.js 沒有完全對應的東西，最接近的是把以下這些合在一起：

| launchSettings.json 的功能 | Node.js 的做法 |
|---|---|
| 設定埠號 | `"start": "PORT=3000 node index.js"` |
| 設定環境 | `"dev": "NODE_ENV=development node index.js"` |
| 開發用環境變數 | `.env.development` |
| 開瀏覽器 | Vite 的 `--open` 參數 |

### 重要事項

- `launchSettings.json` **只在本地開發時有效**，部署時完全不會用到
- 它會被提交到版控（不像 `.env` 通常被 `.gitignore`），因為裡面不應該包含機密資訊
- 本專案在 Docker 開發環境下主要由 `docker-compose.yml` 控制埠號和環境變數，所以 `launchSettings.json` 的角色相對次要

---

## 7. 本專案的三個 NuGet 套件解讀

本專案的 `.csproj` 只安裝了三個第三方套件。這是因為 ASP.NET Core 本身已經內建了大量功能（路由、DI、中介軟體等），不像 Express 需要額外安裝一堆套件。

### 1. Microsoft.AspNetCore.Authentication.JwtBearer

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
```

**做什麼**：處理 JWT（JSON Web Token）驗證。從 HTTP 請求的 `Authorization: Bearer <token>` 標頭中取出 token，驗證簽章、有效期限、發行者等。

**Node.js 等價**：`passport` + `passport-jwt`，或是 `express-jwt`

```javascript
// Node.js (express-jwt)
app.use(expressjwt({
  secret: jwksRsa.expressJwtSecret({ jwksUri: '...' }),
  algorithms: ['ES256']
}));
```

```csharp
// C# (本專案 Program.cs)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.Authority = supabaseUrl + "/auth/v1";
        // ... 驗證參數
    });
```

**本專案用途**：驗證前端從 Supabase Auth 取得的 JWT。後端透過 Supabase 的 JWKS（JSON Web Key Set）端點取得公鑰來驗證 token 簽章（ES256 演算法）。

---

### 2. Npgsql.EntityFrameworkCore.PostgreSQL

```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
```

**做什麼**：這其實是兩個東西的組合——**Entity Framework Core**（ORM）加上 **Npgsql**（PostgreSQL 驅動程式）。讓你用 C# 物件來操作 PostgreSQL 資料庫，而不是寫原始 SQL。

**Node.js 等價**：`Prisma` + `pg`，或是 `TypeORM` + `pg`

```javascript
// Node.js (Prisma)
const readings = await prisma.reading.findMany({
  where: { userId: user.id },
  orderBy: { createdAt: 'desc' }
});
```

```csharp
// C# (Entity Framework Core)
var readings = await _context.Readings
    .Where(r => r.UserId == userId)
    .OrderByDescending(r => r.CreatedAt)
    .ToListAsync();
```

**本專案用途**：連接 Supabase 的 PostgreSQL 資料庫，管理 `profiles` 和 `readings` 兩張資料表的 CRUD 操作。`TarotDbContext` 就是 EF Core 的資料庫上下文，類似 Prisma 的 `PrismaClient`。

---

### 3. Swashbuckle.AspNetCore

```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
```

**做什麼**：自動從你的 Controller 程式碼生成 Swagger / OpenAPI 文件，並提供一個互動式的 API 測試介面（Swagger UI）。

**Node.js 等價**：`swagger-jsdoc` + `swagger-ui-express`

```javascript
// Node.js
app.use('/api-docs', swaggerUi.serve, swaggerUi.setup(swaggerSpec));
```

```csharp
// C# (本專案 Program.cs)
builder.Services.AddSwaggerGen();
app.UseSwagger();
app.UseSwaggerUI();
```

**本專案用途**：在開發環境提供 `/swagger` 頁面，可以直接在瀏覽器裡測試 API。正式環境下會被停用。本地開發時可以在 `http://localhost:5098/swagger` 存取。

### 對照總表

| NuGet 套件 | 功能 | Node.js 等價 |
|---|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT 驗證 | `passport-jwt` / `express-jwt` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | ORM + PostgreSQL 驅動 | `Prisma` + `pg` / `TypeORM` + `pg` |
| `Swashbuckle.AspNetCore` | Swagger API 文件 | `swagger-jsdoc` + `swagger-ui-express` |

> **值得注意的是**：一個 Express.js 專案要達到同樣功能，`package.json` 裡可能需要 15-20 個套件（express、cors、helmet、morgan、dotenv、passport、jsonwebtoken、pg、prisma...）。ASP.NET Core 只需要 3 個額外套件，因為框架本身已經內建了路由、CORS、DI 容器、日誌、設定管理等功能。這也是 .NET 「大框架」與 Node.js 「小核心 + 大量套件」哲學差異的體現。
