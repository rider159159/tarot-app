# 塔羅牌 Web App — 技術棧完整說明（Phase 0 ~ 2）

> 目標讀者：有 Vue + Node.js 後端經驗的開發工程師
> 涵蓋範圍：Phase 0（開發環境）、Phase 1（Supabase Auth + 資料表）、Phase 2（後端 API + 塔羅核心 + 前端功能）

---

## 1. 整體架構

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│   SvelteKit     │─────▶│  .NET Web API    │─────▶│   Supabase      │
│   (前端 SPA)    │      │  (後端 API)       │      │  (DB + Auth)    │
│  Cloudflare     │      │  Azure App       │      │  supabase.co    │
│  Pages          │      │  Service         │      │                 │
└─────────────────┘      └──────────────────┘      └─────────────────┘
        │                                                    ▲
        │ Supabase Auth（登入 / 註冊）                        │
        └────────────────────────────────────────────────────┘
```

**分工原則：**
- 前端直接用 Supabase Auth 做登入 / 註冊（不經過 .NET）
- 前端拿到 JWT token 後，打 .NET API 帶 `Authorization: Bearer <token>`
- .NET API 向 Supabase JWKS 驗證 JWT，確認身份後執行商業邏輯
- .NET API 用獨立 PostgreSQL 連線（Npgsql）直接讀寫 Supabase DB

---

## 2. 技術棧速查

| 項目 | 選擇 | 版本 | Vue 生態對照 |
|------|------|------|-------------|
| 前端框架 | **SvelteKit 2 + Svelte 5** | 2.0 / 5.0 | Nuxt 3 + Vue 3 |
| 前端建構 | **Vite 6** | 6.0 | 同，Vite |
| 前端部署模式 | **adapter-node**（Node.js Server） | 5.5 | Nuxt SSR / Node |
| 套件管理（前端） | **pnpm** | 9.15 | npm / yarn |
| 後端框架 | **ASP.NET Core 8**（Controller 風格） | 8.0 | Express / Koa |
| 套件管理（後端） | **NuGet** | — | npm |
| 認證系統 | **Supabase Auth** | 2.0 | Firebase Auth |
| 資料庫 | **Supabase PostgreSQL** + RLS | — | Firebase Firestore |
| 後端 ORM | **Entity Framework Core + Npgsql** | 8.0 | Prisma / Sequelize |
| 後端認證驗證 | **JWT Bearer（Supabase JWKS）** | — | passport-jwt |
| API 文件 | **Swagger / OpenAPI**（Swashbuckle） | 6.5 | swagger-jsdoc |
| 容器化 | **Docker Compose**（前端 + 後端） | — | 同 |
| Node.js 版本 | **22.12.0** | — | — |
| Runtime（前端） | Node.js 22 LTS | — | — |
| Runtime（後端） | .NET 8 SDK | — | — |

---

## 3. Svelte 5 vs Vue 3 — 關鍵差異速查

> 如果你寫過 Vue 3 Composition API，上手 Svelte 5 會很快。

| 概念 | Vue 3 | Svelte 5 |
|------|-------|----------|
| 響應式變數 | `ref()` / `reactive()` | `$state()` |
| 計算屬性 | `computed()` | `$derived()` |
| 副作用 | `watch()` / `watchEffect()` | `$effect()` |
| Props 接收 | `defineProps()` | `$props()` |
| 模板語法 | `<template>` + `v-if` / `v-for` | 直接寫 HTML + `{#if}` / `{#each}` |
| 雙向綁定 | `v-model` | `bind:value` |
| 事件 | `@click` | `onclick` |
| 插槽 | `<slot>` | `{@render children()}` (Svelte 5 snippets) |
| 生命週期 | `onMounted()` | `$effect()` (自動追蹤) |
| 狀態管理 | Pinia | Svelte Runes（module-level `$state`） |
| 路由 | Vue Router / Nuxt pages | SvelteKit 檔案系統路由 (`routes/`) |

**範例對比 — 響應式計數器：**

```vue
<!-- Vue 3 -->
<script setup>
import { ref } from 'vue'
const count = ref(0)
</script>
<template>
  <button @click="count++">{{ count }}</button>
</template>
```

```svelte
<!-- Svelte 5 -->
<script>
let count = $state(0)
</script>
<button onclick={() => count++}>{count}</button>
```

---

## 4. 專案結構

```
tarot-app/
├── docker-compose.yml        # 前端 + 後端統一啟動
├── .env / .env.example       # 環境變數（Supabase keys 等）
├── frontend/                 # SvelteKit App（adapter-node）
│   ├── Dockerfile            # Node 22-slim + pnpm
│   ├── svelte.config.js      # adapter-node，build 輸出到 build/
│   ├── vite.config.ts        # Docker HMR 設定（usePolling、host 0.0.0.0）
│   ├── tsconfig.json         # TypeScript strict 模式
│   └── src/
│       ├── routes/           # 頁面路由（類似 Nuxt pages/）
│       │   ├── +layout.js    # export const ssr = false
│       │   ├── +layout.svelte# Auth guard + Navbar 顯示邏輯
│       │   ├── +page.svelte  # 首頁（塔羅抽牌）
│       │   ├── login/        # 登入頁
│       │   ├── register/     # 註冊頁
│       │   ├── auth/callback/# Supabase OAuth callback
│       │   ├── history/      # 抽牌歷史紀錄頁
│       │   └── profile/      # 個人資料頁
│       └── lib/
│           ├── supabase.ts   # Supabase client（createBrowserClient from @supabase/ssr）
│           ├── api.ts        # .NET API fetch wrapper（自動帶 JWT）
│           ├── stores/
│           │   └── auth.svelte.ts  # Auth 狀態管理（Svelte 5 runes）
│           ├── tarot/        # 塔羅牌核心資料 & 邏輯
│           ├── components/
│           │   └── Navbar.svelte   # 頂部導覽列
│           ├── utils/        # 共用工具函式
│           └── types/        # TypeScript 型別定義
├── backend/                  # ASP.NET Core 8
│   ├── Dockerfile            # .NET 8 SDK，dotnet watch
│   ├── TarotApi.sln
│   └── TarotApi/
│       ├── TarotApi.csproj   # NuGet 套件設定
│       ├── Program.cs        # 服務註冊 + Middleware pipeline
│       ├── appsettings.json
│       ├── Controllers/
│       │   ├── ReadingController.cs  # /api/readings（CRUD + stats）
│       │   ├── ProfileController.cs  # /api/profile（取得 / 更新）
│       │   ├── TarotController.cs    # /api/tarot/cards（牌資料，不需 auth）
│       │   └── HealthController.cs   # /api/health（health check）
│       ├── Services/
│       │   ├── ReadingService.cs     # 抽牌紀錄 CRUD + 統計
│       │   ├── ProfileService.cs     # 個人資料 CRUD
│       │   └── TarotService.cs       # 抽牌邏輯 + 牌陣定義
│       ├── Data/
│       │   ├── TarotDbContext.cs     # EF Core DbContext
│       │   └── TarotCards.cs        # 78 張牌靜態資料（hardcoded）
│       ├── Models/
│       │   ├── Profile.cs
│       │   ├── Reading.cs
│       │   ├── SpreadType.cs        # Enum: Single / ThreeCard / CelticCross
│       │   ├── SpreadPosition.cs    # Record: (Index, Label, Description)
│       │   └── Dtos/               # 所有 Request / Response DTO
│       └── Extensions/
│           └── ClaimsPrincipalExtensions.cs  # JWT 解析 user ID
└── database/
    └── migrations/
        ├── 001_create_readings.sql   # (Phase 0，已被 002 取代)
        └── 002_auth_tables.sql       # profiles + readings + RLS + trigger
```

---

## 5. Docker 開發環境

### 5.1 整體設計

`docker compose up --build` 一次啟動前後端，兩個服務各自支援 hot reload：

```yaml
services:
  frontend:            # Node 22 + pnpm dev
    ports: 5173:5173
    volumes:
      - ./frontend:/app          # 原始碼掛入
      - /app/node_modules        # anonymous volume，避免被 host 覆蓋
      - ./.env:/app/.env:ro      # 環境變數（readonly）
  backend:             # .NET 8 SDK + dotnet watch
    ports: 5098:5098
    volumes:
      - ./backend:/app           # 原始碼掛入
      - /app/TarotApi/bin        # anonymous volume，排除編譯產物
      - /app/TarotApi/obj        # anonymous volume，排除編譯產物
    env_file: .env
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      DOTNET_USE_POLLING_FILE_WATCHER: "true"  # Docker 需要 polling
      DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER: "true"
```

### 5.2 各服務 Dockerfile

**Frontend（`frontend/Dockerfile`）：**

```dockerfile
FROM node:22-slim
RUN corepack enable          # 啟用 corepack，讓 pnpm 可用
WORKDIR /app
COPY package.json pnpm-lock.yaml* ./
RUN pnpm install --frozen-lockfile || pnpm install
COPY . .
EXPOSE 5173
CMD ["pnpm", "dev"]
```

**Backend（`backend/Dockerfile`）：**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY TarotApi.sln ./
COPY TarotApi/TarotApi.csproj ./TarotApi/
RUN dotnet restore            # 預先還原，利用 Docker layer cache
COPY . .
EXPOSE 5098
CMD ["dotnet", "watch", "run", "--project", "TarotApi", "--urls", "http://0.0.0.0:5098"]
```

### 5.3 Hot Reload 細節

| 環境 | 工具 | 對照 Node.js |
|------|------|-------------|
| 前端 HMR | Vite + `usePolling: true` | 同（Vite HMR） |
| 後端 hot reload | `dotnet watch` | nodemon |
| Frontend HMR port | `hmr.clientPort: 5173` | — |
| 前端 polling 原因 | Linux 容器不支援 macOS FSEvents | 同理 |

```typescript
// vite.config.ts — Docker HMR 設定
server: {
  host: '0.0.0.0',      // 監聽所有網卡（讓 host 機能連進來）
  port: 5173,
  strictPort: true,
  watch: { usePolling: true },   // 避免 inotify 問題
  hmr: { clientPort: 5173 }      // 瀏覽器連 WS 的 port
}
```

---

## 6. 前端技術詳解

### 6.1 SvelteKit 設定

**`svelte.config.js`：**
```javascript
import adapter from '@sveltejs/adapter-node';
export default {
  kit: {
    adapter: adapter({ out: 'build' })
  }
};
```

- `adapter-node`：編譯成 Node.js server，適合部署到有 runtime 的環境（非純靜態）
- 與 `adapter-static` 的差異：`adapter-node` 有 server-side 能力（未來可加 SSR）

**`routes/+layout.js`：**
```javascript
export const ssr = false;  // 強制 client-side rendering（SPA 行為）
```

### 6.2 Supabase Client（前端）

```typescript
// src/lib/supabase.ts
import { createBrowserClient } from '@supabase/ssr';

export const supabase = createBrowserClient(
  import.meta.env.PUBLIC_SUPABASE_URL,
  import.meta.env.PUBLIC_SUPABASE_ANON_KEY
);
```

- 使用 `@supabase/ssr` 套件的 `createBrowserClient`（相比原始 `@supabase/supabase-js` 的 `createClient`，更適合 SvelteKit 環境）
- env vars 為空時 graceful 處理，不會在啟動時噴錯

### 6.3 Auth Store（Svelte 5 runes 狀態管理）

```typescript
// src/lib/stores/auth.svelte.ts
// Svelte 5 module-level reactive state（類比 Pinia 的 defineStore）
let user = $state(null);
let session = $state(null);
let loading = $state(true);

// 監聽 Supabase auth 狀態變化（類比 Firebase onAuthStateChanged）
supabase.auth.onAuthStateChange((_event, newSession) => {
  session = newSession;
  user = newSession?.user ?? null;
});

// 匯出 reactive getter
export const auth = {
  get user() { return user; },
  get session() { return session; },
  login, register, logout
};
```

### 6.4 API Wrapper（自動帶 JWT）

```typescript
// src/lib/api.ts
async function getAuthHeaders() {
  const { data } = await supabase.auth.getSession();
  return { Authorization: `Bearer ${data.session?.access_token}` };
}

export async function apiGet<T>(path: string): Promise<T> { ... }
export async function apiPost<T>(path: string, body: unknown): Promise<T> { ... }
export async function apiPut<T>(path: string, body: unknown): Promise<T> { ... }
export async function apiDelete(path: string): Promise<void> { ... }
```

- 類比 axios instance with interceptors
- `PUBLIC_API_BASE_URL` 控制後端 URL（預設 `http://localhost:5098`）

### 6.5 環境變數前綴

> SvelteKit 的 `PUBLIC_` 前綴 ≈ Vue/Vite 的 `VITE_` 前綴

`PUBLIC_` 開頭的變數會被打包進 client bundle，可在瀏覽器端存取；
沒有 `PUBLIC_` 的只能在 server-side 存取。

---

## 7. 後端技術詳解（ASP.NET Core 8）

### 7.1 NuGet 套件

| 套件 | 版本 | 用途 | Node.js 對照 |
|------|------|------|-------------|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.11 | JWT 驗證中介層 | passport-jwt |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.11 | EF Core + PostgreSQL driver | Prisma + pg |
| `Swashbuckle.AspNetCore` | 6.5.0 | Swagger / OpenAPI 文件 | swagger-jsdoc |

### 7.2 Program.cs — 服務註冊與 Middleware

```csharp
// 服務註冊（類比 Express 的設定區段）
builder.Services.AddControllers();           // 啟用 MVC Controller（類比 Express Router）
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();            // API 文件
builder.Services.AddCors(...);               // CORS 設定
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(...);                      // JWT 驗證
builder.Services.AddDbContext<TarotDbContext>(...);   // EF Core + PostgreSQL
builder.Services.AddSingleton<TarotService>();        // 牌陣服務（Singleton）
builder.Services.AddScoped<ReadingService>();          // 抽牌紀錄（Scoped）
builder.Services.AddScoped<ProfileService>();          // 個人資料（Scoped）

// Middleware Pipeline（順序重要）
app.UseSwagger();           // 開發環境才啟用
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.UseAuthentication();    // 解析 JWT
app.UseAuthorization();     // 執行權限檢查
app.MapControllers();
```

### 7.3 JWT 驗證（Supabase JWKS）

後端不存放 JWT secret，改用 JWKS（JSON Web Key Set）動態取得 Supabase 的公鑰：

```csharp
.AddJwtBearer(options => {
    options.Authority = $"{supabaseUrl}/auth/v1";
    options.MetadataAddress = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidIssuer = $"{supabaseUrl}/auth/v1",
        ValidateAudience = true,
        ValidAudience = "authenticated",    // Supabase JWT 的固定 audience
        ValidateLifetime = true,
        ValidAlgorithms = new[] { "ES256" } // Supabase 用 ES256（非對稱，非 HS256）
    };
});
```

**與傳統 JWT 的差異：**
- Supabase 用 ES256（Elliptic Curve），不是常見的 HS256（HMAC）
- 透過 JWKS endpoint 自動取得公鑰，不需手動管理 secret
- `audience` 固定為 `"authenticated"`（Supabase 規範）

**從 JWT 取 user ID：**

```csharp
// Extensions/ClaimsPrincipalExtensions.cs
public static Guid GetUserId(this ClaimsPrincipalExtensions user) {
    var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value  // "sub" claim
           ?? user.FindFirst("sub")?.Value;
    return Guid.Parse(sub!);
}
```

### 7.4 Controller 設計

全域 `[Authorize]` filter，所有 endpoint 預設需要登入：

```csharp
// Program.cs
builder.Services.AddControllers(options => {
    options.Filters.Add(new AuthorizeFilter());  // 預設全部需要 auth
});
```

| Controller | Route | 說明 |
|-----------|-------|------|
| `ReadingController` | `/api/readings` | 抽牌紀錄 CRUD + 統計 |
| `ProfileController` | `/api/profile` | 個人資料讀取與更新 |
| `TarotController` | `/api/tarot` | 牌資料查詢（`[AllowAnonymous]`） |
| `HealthController` | `/api/health` | Health check（`[AllowAnonymous]`） |

**API 端點一覽：**

```
POST   /api/readings              → 建立抽牌紀錄（需 JWT）
GET    /api/readings              → 取得歷史紀錄（分頁：page, pageSize）
GET    /api/readings/{id}         → 取得單筆紀錄
GET    /api/readings/stats        → 取得統計（最常出現的牌、牌陣使用比例）
DELETE /api/readings/{id}         → 刪除紀錄

GET    /api/profile               → 取得個人資料
PUT    /api/profile               → 更新 display name

GET    /api/tarot/cards           → 取得所有牌（摘要）
GET    /api/tarot/cards/{id}      → 取得單張牌詳細資料

GET    /api/health                → Health check → { status: "ok", timestamp: "..." }
```

### 7.5 塔羅牌資料（TarotCards.cs）

78 張牌以靜態 class 硬碼存於 `Data/TarotCards.cs`：

```csharp
public static class TarotCards {
    public static readonly List<TarotCardInfo> All = new() {
        new("major_00_fool",     "The Fool",      "愚者",   Arcana.Major, null,  0,
            "新的開始、純真、冒險", "魯莽、逃避、不成熟",
            new[] { "新開始", "冒險", "純真" }),
        // ... 共 78 張
    };
}
```

**牌組結構：**
- **Major Arcana**：22 張（愚者 0 ~ 世界 21），含完整中英文名稱
- **Minor Arcana**：56 張，分 4 組
  - 權杖（Wands）：14 張（Ace ~ King）
  - 聖杯（Cups）：14 張
  - 寶劍（Swords）：14 張
  - 錢幣（Pentacles）：14 張

每張牌包含：`Id`、`Name`（英）、`NameCht`（繁中）、`Arcana`、`Suit`、`Number`、`MeaningUpright`、`MeaningReversed`、`Keywords[]`

### 7.6 抽牌演算法（TarotService.cs）

```csharp
public List<DrawnCard> DrawCards(SpreadType spreadType) {
    var count = spreadType switch {
        SpreadType.Single      => 1,
        SpreadType.ThreeCard   => 3,
        SpreadType.CelticCross => 10,
        _ => throw new ArgumentException()
    };

    // Fisher-Yates shuffle with cryptographic RNG
    var shuffled = TarotCards.All.ToList();
    for (int i = shuffled.Count - 1; i > 0; i--) {
        int j = RandomNumberGenerator.GetInt32(i + 1);  // 密碼學等級亂數
        (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
    }

    return shuffled.Take(count).Select((card, idx) => new DrawnCard {
        CardId = card.Id,
        Orientation = RandomNumberGenerator.GetInt32(2) == 0 ? "upright" : "reversed",
        PositionIndex = idx
    }).ToList();
}
```

- 使用 `System.Security.Cryptography.RandomNumberGenerator`（非 `Random.Shared`）
- 正逆位也用 cryptographic RNG 決定（50/50 機率）

### 7.7 EF Core + JSONB 資料存取

**DbContext：**

```csharp
public class TarotDbContext : DbContext {
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Reading> Readings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Reading>()
            .Property(r => r.Cards)
            .HasColumnType("jsonb");   // 告訴 EF 這是 JSONB 欄位
    }
}
```

**Reading Model 的 cards 欄位：**

```csharp
public class Reading {
    public JsonDocument Cards { get; set; }  // 對應 PostgreSQL JSONB
}
```

存入 DB 的 JSON 結構：
```json
[
  { "card_id": "major_01_magician", "orientation": "upright",   "position_index": 0 },
  { "card_id": "minor_cups_03",     "orientation": "reversed",  "position_index": 1 }
]
```

**JSONB 統計查詢（raw SQL）：**

EF Core 不能直接處理 JSONB 陣列展開，改用 raw SQL：

```csharp
// ReadingService.cs — 查詢每張牌出現次數
var sql = @"
    SELECT elem->>'card_id' as card_id, COUNT(*) as count
    FROM readings, jsonb_array_elements(cards) as elem
    WHERE user_id = @userId
    GROUP BY card_id
    ORDER BY count DESC
    LIMIT 5";
```

### 7.8 CORS 設定

```csharp
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        var origins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
            ?.Split(',') ?? new[] { "http://localhost:5173" };
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

- `ALLOWED_ORIGINS` 可設定多個 origin（逗號分隔）
- 開發時預設允許 `http://localhost:5173`

---

## 8. Supabase 連線方式

### 8.1 前端：Supabase JS Client（Auth + 直查）

```
前端 → Supabase Auth API → 登入 / 註冊
前端 → Supabase DB（anon key + RLS）→ 存取資料
```

- 使用 `PUBLIC_SUPABASE_ANON_KEY`（公開 key，安全性由 RLS 保障）
- RLS（Row Level Security）在 DB 層過濾資料，即使 key 外洩也無法跨 user 存取

### 8.2 後端：直連 PostgreSQL（Npgsql + EF Core）

```
後端 → PostgreSQL（直連） → SUPABASE_DB_CONNECTION_STRING
```

- 使用 Session Pooler 或 Direct Connection 字串（Supabase Dashboard > Database Settings）
- 格式：`Host=db.xxx.supabase.co;Database=postgres;Username=postgres;Password=xxx`
- 後端不走 Supabase JS Client，直接走標準 PostgreSQL 協定

### 8.3 後端：JWT 驗證（JWKS）

```
前端 → 取得 Supabase JWT → 打後端 API（帶 Bearer token）
後端 → 向 Supabase JWKS endpoint 驗證 token → 解析 user ID
```

- 後端 **不需要** 持有 `SUPABASE_ANON_KEY`
- 只需 `PUBLIC_SUPABASE_URL` 來組 JWKS endpoint URL

### 8.4 資料庫 Schema

#### profiles 表
```sql
CREATE TABLE profiles (
  id           UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  display_name TEXT NOT NULL,
  created_at   TIMESTAMPTZ DEFAULT now(),
  updated_at   TIMESTAMPTZ DEFAULT now()
);
-- RLS: 只能讀取 / 更新自己的 profile
-- Trigger: 註冊時自動建立（取 email @ 前面當 display_name）
```

#### readings 表（塔羅抽牌紀錄）
```sql
CREATE TABLE readings (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id        UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  spread_type    TEXT NOT NULL CHECK (spread_type IN ('single', 'three-card', 'celtic-cross')),
  question       TEXT,
  cards          JSONB NOT NULL,    -- 抽到的牌陣列，含正逆位
  interpretation TEXT,
  notes          TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);
-- RLS: SELECT / INSERT / UPDATE / DELETE 全部限制 auth.uid() = user_id
-- Index: (user_id, created_at DESC) 加速分頁查詢
```

**RLS 概念對照：**

| Supabase RLS | Firestore Rules |
|---|---|
| `auth.uid() = user_id` | `request.auth.uid == resource.data.userId` |
| DB 層過濾，無法繞過 | Security Rules 層過濾 |
| 適用所有連線（SDK / 直連） | 僅適用 Firestore SDK |

> 注意：後端走 `SUPABASE_DB_CONNECTION_STRING` 直連時，預設以 `postgres` superuser 身份連線，**RLS 不生效**。後端自己在 Service 層過濾 `user_id = @userId` 確保安全。

---

## 9. 環境變數

```env
# ── 前端用（PUBLIC_ 前綴，可在 client bundle 存取）──
PUBLIC_SUPABASE_URL=https://xxx.supabase.co
PUBLIC_SUPABASE_ANON_KEY=eyJ...
PUBLIC_API_BASE_URL=http://localhost:5098

# ── 後端用（不對外，僅 server 端）──
SUPABASE_JWT_SECRET=your-jwt-secret        # （備用，目前走 JWKS 不需要）
SUPABASE_DB_CONNECTION_STRING=postgresql://postgres:xxx@db.xxx.supabase.co:5432/postgres

# ── CORS（後端）──
ALLOWED_ORIGINS=http://localhost:5173      # 多個 origin 用逗號分隔
```

**`.env` 位置說明：**
- `.env` 放在 repo 根目錄
- `frontend/` 下有 symlink `frontend/.env -> ../.env`
- Docker 把根目錄 `.env` 掛進兩個容器

---

## 10. 開發指令

```bash
# ── 啟動全部（Docker）──
docker compose up --build

# ── 或分開跑 ──
cd frontend && pnpm dev          # http://localhost:5173
cd backend/TarotApi && dotnet watch run --urls http://localhost:5098

# ── TypeScript 型別檢查（前端）──
cd frontend && pnpm check

# ── 打包前端 ──
cd frontend && pnpm build        # 輸出到 frontend/build/

# ── API 開發測試 ──
open http://localhost:5098/swagger       # Swagger UI（只在 Development 環境）
curl http://localhost:5098/api/health    # Health check
```

---

## 11. 驗證清單

### Phase 0 + 1（基礎環境 + Auth）
- [x] `docker compose up` 前後端都啟動
- [x] http://localhost:5173 看到前端頁面
- [x] http://localhost:5098/api/health 回傳 `{ status: "ok" }`
- [x] http://localhost:5098/swagger 看到 Swagger UI
- [x] 修改 `.svelte` 檔 → 頁面自動更新（HMR）
- [x] 修改 `.cs` 檔 → API 自動重新編譯（dotnet watch）
- [x] 可以註冊新帳號（Supabase Auth）
- [x] 可以登入 / 登出
- [x] 未登入時自動導向登入頁

### Phase 2（後端 API + 塔羅功能）
- [x] `POST /api/readings`（帶 JWT）→ 成功建立抽牌紀錄
- [x] `GET /api/readings` → 取得歷史紀錄（分頁）
- [x] `GET /api/readings/stats` → 取得統計資料
- [x] `GET /api/profile` → 取得個人資料
- [x] `PUT /api/profile` → 更新 display name
- [x] 未帶 token 打 API → 401 Unauthorized
- [x] 帶其他 user 的 token → 只能看到自己的資料

---

## 12. Phase Roadmap

| Phase | 狀態 | 主要內容 |
|-------|------|---------|
| Phase 0 | 完成 | Docker Compose 開發環境、SvelteKit 初始化、.NET API 骨架 |
| Phase 1 | 完成 | Supabase Auth 整合、profiles / readings 資料表、前端登入系統 |
| Phase 2 | 完成 | JWT 驗證中介層、EF Core + PostgreSQL、78 張牌資料、3 種牌陣、歷史紀錄頁、個人資料頁 |
| Phase 3 | 計劃中 | 部署（Cloudflare Pages + Azure App Service） |
| Phase 4 | 計劃中 | Capacitor iOS 打包 |
