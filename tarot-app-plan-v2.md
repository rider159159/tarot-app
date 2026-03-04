# 塔羅牌 Web App 專案規劃（含 .NET 後端）

## 技術棧確認

| 項目 | 選擇 |
|------|------|
| 前端框架 | SvelteKit（adapter-static） |
| 後端 API | ASP.NET Core Web API（.NET 8） |
| 認證系統 | Supabase Auth |
| 資料庫 | Supabase PostgreSQL |
| 前端部署 | Cloudflare Pages |
| 後端部署 | Azure App Service（Free F1） |
| App 打包 | Capacitor（後續 Phase） |
| 開發環境 | Docker Compose（前後端統一環境） |

## 架構說明

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│                 │      │                  │      │                 │
│   SvelteKit     │─────▶│  .NET Web API    │─────▶│   Supabase      │
│   (前端 SPA)    │      │  (後端 API)       │      │  (DB + Auth)    │
│                 │      │                  │      │                 │
│  Cloudflare     │      │  Azure App       │      │  supabase.co    │
│  Pages          │      │  Service         │      │                 │
└─────────────────┘      └──────────────────┘      └─────────────────┘

前端職責：UI、路由、呼叫 API、Auth 狀態管理
後端職責：商業邏輯、塔羅牌抽牌演算法、歷史紀錄 CRUD、資料驗證
Supabase：資料庫儲存、Auth token 驗證（JWT）
```

### 前後端分工原則

- 前端直接用 Supabase Auth 做登入/註冊（不經過 .NET）
- 前端拿到 JWT token 後，每次打 .NET API 都帶上 Authorization header
- .NET API 驗證 Supabase JWT，確認用戶身份後才執行操作
- .NET API 用 Supabase client 或直接用 Npgsql 連 PostgreSQL

---

## 執行順序總覽

```
Phase 0 → Docker 開發環境（前端 + 後端）+ 專案初始化
Phase 1 → Supabase 設定 + 前端會員系統
Phase 2 → .NET 後端 API 基礎建設（Auth 驗證、Supabase 串接）
Phase 3 → 塔羅牌核心功能（後端邏輯 + 前端頁面）
Phase 4 → 歷史紀錄 + 個人頁面
Phase 5 → 部署（Cloudflare Pages + Azure App Service）
Phase 6 → Capacitor iOS 打包（後續）
```

---

## Phase 0：Docker 開發環境 + 專案初始化

### 你需要先手動完成

1. 到 [supabase.com](https://supabase.com/dashboard/sign-up) 註冊帳號並建立 Project
2. 到 Settings → API 取得 `Project URL` 和 `anon public key`
3. 到 Settings → Database 取得 `Connection string`（給 .NET 用）
4. 到 Authentication → Settings 關閉「Confirm email」（開發階段）
5. 到 Settings → API → JWT Settings 記下 `JWT Secret`（給 .NET 驗證 token 用）
6. 建立 GitHub repo：`tarot-app`

### Claude Code Prompt — Phase 0

```
我要建立一個塔羅牌 Web App，前後端分離架構。請幫我完成 Phase 0：Docker 開發環境 + 專案初始化。

## 整體架構
- 前端：SvelteKit（adapter-static，之後部署到 Cloudflare Pages）
- 後端：ASP.NET Core 8 Web API（之後部署到 Azure App Service）
- 資料庫/認證：Supabase（PostgreSQL + Auth）
- 本地開發：Docker Compose 統一管理前後端

## 請幫我建立以下內容

### 1. 專案目錄結構
```
tarot-app/
├── docker-compose.yml          # 同時啟動前端 + 後端
├── .gitignore
├── .env.example
│
├── frontend/                   # SvelteKit 專案
│   ├── Dockerfile
│   ├── package.json
│   ├── svelte.config.js        # adapter-static，fallback '200.html'
│   ├── vite.config.ts
│   ├── src/
│   │   ├── app.html
│   │   ├── routes/
│   │   │   └── +page.svelte    # 首頁（暫時 Hello World）
│   │   └── lib/
│   │       ├── supabase.ts     # Supabase client 初始化
│   │       ├── api.ts          # 呼叫 .NET API 的 wrapper
│   │       └── types/
│   │           └── index.ts
│   └── static/
│
├── backend/                    # ASP.NET Core Web API
│   ├── Dockerfile
│   ├── TarotApi.sln
│   └── TarotApi/
│       ├── TarotApi.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── Controllers/
│       │   └── HealthController.cs   # GET /api/health 測試端點
│       └── Properties/
│           └── launchSettings.json
│
└── supabase/
    └── migrations/             # SQL migration 檔案（Phase 1 使用）
```

### 2. Docker Compose 設定
- frontend service：
  - 基於 node:20-slim，安裝 pnpm
  - port 5173:5173
  - 掛載 ./frontend 到容器內
  - 確保 Vite HMR 正常運作（設定 server.host 和 server.watch.usePolling）
- backend service：
  - 基於 mcr.microsoft.com/dotnet/sdk:8.0
  - port 5098:5098
  - 掛載 ./backend 到容器內
  - 使用 dotnet watch run 實現 hot reload
- 兩個 service 共用同一個 Docker network

### 3. 前端 SvelteKit
- 使用 TypeScript
- 安裝 @sveltejs/adapter-static、@supabase/supabase-js
- svelte.config.js 設定 adapter-static，fallback: '200.html'
- src/lib/supabase.ts：從環境變數初始化 Supabase client
- src/lib/api.ts：建立一個 fetch wrapper，之後呼叫 .NET API 用
  - 自動帶上 Supabase session 的 JWT token 作為 Authorization header
  - base URL 從環境變數讀取

### 4. 後端 ASP.NET Core
- 使用 .NET 8，建立 Web API 專案（不要 minimal API，用 Controller 風格）
- Program.cs 設定：
  - CORS：允許 localhost:5173（開發用）
  - Swagger/OpenAPI（開發時方便測試）
  - Controller routing
- HealthController：GET /api/health 回傳 { "status": "ok", "timestamp": "..." }
- appsettings.Development.json 放開發用設定

### 5. 環境變數
.env.example 內容：
```
# Supabase
PUBLIC_SUPABASE_URL=
PUBLIC_SUPABASE_ANON_KEY=
SUPABASE_JWT_SECRET=
SUPABASE_DB_CONNECTION_STRING=

# API
PUBLIC_API_BASE_URL=http://localhost:5098
```

### 6. 驗證清單
- docker compose up 後前端和後端都能啟動
- 瀏覽器開 http://localhost:5173 看到前端頁面
- 瀏覽器開 http://localhost:5098/api/health 看到 JSON 回應
- 瀏覽器開 http://localhost:5098/swagger 看到 Swagger UI
- 前端修改 .svelte 檔案後頁面自動更新
- 後端修改 .cs 檔案後 API 自動重新編譯

不需要任何 UI 美化，Phase 0 只要能跑起來就好。
```

---

## Phase 1：Supabase 設定 + 前端會員系統

### 你需要先手動完成
- 確認 Phase 0 正常運作
- 在 .env 填入你的 Supabase 設定值

### Claude Code Prompt — Phase 1

```
接續 Phase 0 的 tarot-app 專案，請幫我完成 Phase 1：Supabase 資料表 + 前端會員系統。

## 1. Supabase 資料表（產生 SQL migration 檔，放在 supabase/migrations/）

### profiles 表
- id: uuid（references auth.users，primary key）
- display_name: text not null
- created_at: timestamptz default now()
- updated_at: timestamptz default now()
- RLS：用戶只能 select/update 自己的 profile

### readings 表（塔羅牌抽牌紀錄）
- id: uuid (primary key, gen_random_uuid)
- user_id: uuid（references auth.users，not null）
- spread_type: text not null（'single', 'three_card', 'celtic_cross'）
- cards: jsonb not null（抽到的牌、位置、正逆位）
- question: text（nullable，用戶的提問）
- interpretation: text（nullable，牌義解讀文字）
- notes: text（nullable，用戶的筆記）
- created_at: timestamptz default now()
- RLS：用戶只能 CRUD 自己的紀錄

### trigger
- 當 auth.users 新增用戶時，自動在 profiles 建立一筆
- display_name 預設為 email 的 @ 前面部分

### indexes
- readings 表的 user_id + created_at 建立複合索引（查詢歷史紀錄用）

## 2. 前端會員功能

### /login 頁面
- Email + 密碼登入表單
- 登入失敗顯示錯誤訊息
- 連結到 /register

### /register 頁面
- Email + 密碼 + 顯示名稱
- 密碼至少 8 碼
- 註冊成功後自動登入並導向首頁

### Auth 狀態管理
- 建立 src/lib/stores/auth.ts（Svelte store）
- 管理 user、session、loading 狀態
- 提供 login()、register()、logout() 方法
- 自動監聽 Supabase auth state change

### Layout Auth Guard
- 在 src/routes/+layout.svelte 或 +layout.ts 判斷登入狀態
- 未登入 → 自動導向 /login
- /login 和 /register 不受 guard 保護

## 3. 更新 src/lib/api.ts
- 確保每次呼叫 .NET API 都自動帶上最新的 JWT token
- token 過期時自動 refresh

## 4. 技術要求
- SQL migration 檔用時間戳命名（如 20240101000000_create_tables.sql）
- 錯誤處理要完整（網路錯誤、認證錯誤等）
- UI 用基本 HTML + 簡單 CSS，不需要 UI 框架
- TypeScript 嚴格型別

## 5. 驗證清單
- 到 Supabase SQL Editor 執行 migration 後，Table Editor 能看到 profiles 和 readings 表
- 能正常註冊新帳號，profiles 自動建立
- 能登入/登出
- 未登入時被導向 /login
- 登入後回到首頁
```

---

## Phase 2：.NET 後端 API 基礎建設

### Claude Code Prompt — Phase 2

```
接續前面的 tarot-app 專案，請幫我完成 Phase 2：.NET Web API 的基礎建設。

## 目標
讓 .NET API 能驗證 Supabase 的 JWT token，並連接 Supabase PostgreSQL。

## 1. NuGet 套件安裝
- Microsoft.AspNetCore.Authentication.JwtBearer（JWT 驗證）
- Npgsql.EntityFrameworkCore.PostgreSQL（EF Core + PostgreSQL）
- Swashbuckle.AspNetCore（Swagger，Phase 0 應該已有）

## 2. JWT 認證設定
在 Program.cs 加入 JWT Bearer Authentication：
- 使用 Supabase 的 JWT Secret 驗證 token
- Issuer 設為 Supabase project URL
- 驗證 token 的 aud、iss、exp
- 所有 /api/* 端點預設需要認證（除了 /api/health）

## 3. Entity Framework Core 設定
### DbContext
- 建立 TarotDbContext，包含 Profiles 和 Readings 兩個 DbSet
- Connection string 從環境變數/appsettings 讀取

### Entity Models（對應 Supabase 表）
```
backend/TarotApi/
├── Models/
│   ├── Profile.cs          # 對應 profiles 表
│   ├── Reading.cs           # 對應 readings 表
│   └── Dtos/
│       ├── ReadingCreateDto.cs    # 建立 reading 的輸入
│       ├── ReadingResponseDto.cs  # 回傳 reading 的輸出
│       └── ProfileDto.cs
```

### 重要
- Entity 的 table name 要對應 Supabase 的 public schema（小寫 + 底線）
- 不要用 EF Migration 建表（表已經在 Supabase 建好了）
- EF Core 只負責讀寫，不負責 schema 管理

## 4. 使用者身份取得
建立一個 helper 或 extension method：
- 從 JWT claims 中取得 user_id（sub claim）
- 建立 GetCurrentUserId() 方法給 Controller 使用

## 5. 專案結構
```
backend/TarotApi/
├── Program.cs                 # DI、middleware、auth 設定
├── Data/
│   └── TarotDbContext.cs
├── Models/
│   ├── Profile.cs
│   ├── Reading.cs
│   └── Dtos/
├── Controllers/
│   ├── HealthController.cs    # [AllowAnonymous]
│   └── (Phase 3 加入更多 controller)
├── Extensions/
│   └── ClaimsPrincipalExtensions.cs  # GetCurrentUserId()
├── Middleware/
│   └── (如有需要)
└── Services/
    └── (Phase 3 加入業務邏輯)
```

## 6. CORS 更新
- 開發環境允許 http://localhost:5173
- 之後部署時要改為 Cloudflare Pages 的網域

## 7. appsettings 設定結構
```json
{
  "Supabase": {
    "JwtSecret": "",
    "DbConnectionString": ""
  },
  "AllowedOrigins": ["http://localhost:5173"]
}
```
開發時從 .env 或 appsettings.Development.json 讀取，
部署時用 Azure App Service 的環境變數覆蓋。

## 8. 驗證清單
- /api/health 不需要 token 就能訪問
- 不帶 token 打其他 API → 回 401
- 前端登入後，用 api.ts 打 .NET API → 能拿到 200 回應
- Swagger UI 能加入 Bearer token 測試
- EF Core 能正常連到 Supabase PostgreSQL 並查詢
```

---

## Phase 3：塔羅牌核心功能

### Claude Code Prompt — Phase 3

```
接續前面的 tarot-app 專案，請幫我完成 Phase 3：塔羅牌核心功能。

## 1. 後端：塔羅牌資料與邏輯

### 塔羅牌靜態資料
建立 backend/TarotApi/Data/TarotCards.cs（靜態類別）：
- 78 張完整塔羅牌資料
  - 22 張大阿爾克那（Major Arcana，0-21）
  - 56 張小阿爾克那（4 花色 × 14 張）
- 每張牌的資料結構：
  ```csharp
  public record TarotCardInfo
  {
      public string Id { get; init; }            // "major_00_fool"
      public string Name { get; init; }          // "The Fool"
      public string NameCht { get; init; }       // "愚者"
      public string Arcana { get; init; }        // "major" | "minor"
      public string? Suit { get; init; }         // "wands"|"cups"|"swords"|"pentacles"
      public int Number { get; init; }
      public string MeaningUpright { get; init; }    // 正位牌義（繁體中文，2-3 句）
      public string MeaningReversed { get; init; }   // 逆位牌義（繁體中文，2-3 句）
      public string[] Keywords { get; init; }        // 關鍵詞（繁體中文）
  }
  ```
- 牌義內容要完整有品質，不要 placeholder

### 抽牌 Service
建立 backend/TarotApi/Services/TarotService.cs：
- DrawCards(SpreadType type) → 根據牌陣抽牌
- 支援三種牌陣：
  - Single（1 張：每日一牌）
  - ThreeCard（3 張：過去/現在/未來）
  - CelticCross（10 張：各有明確位置名稱）
- 每張牌 50% 機率正位/逆位
- 使用 RandomNumberGenerator（密碼學安全隨機）
- 同一次抽牌不會重複抽到同一張

### Reading Service
建立 backend/TarotApi/Services/ReadingService.cs：
- CreateReading()：抽牌 + 存入資料庫
- GetReadings()：取得用戶的歷史紀錄（分頁）
- GetReadingById()：取得單筆紀錄
- DeleteReading()：刪除紀錄
- 所有操作都驗證 user_id

### Controllers
建立 backend/TarotApi/Controllers/：

#### TarotController.cs
- GET /api/tarot/cards → 回傳所有牌的基本資訊（不含牌義，給前端展示用）
- GET /api/tarot/cards/{id} → 回傳單張牌的完整資訊

#### ReadingController.cs
- POST /api/readings → 建立新的抽牌（帶入 spread_type 和 question）
- GET /api/readings → 取得歷史紀錄（支援分頁：page、pageSize）
- GET /api/readings/{id} → 取得單筆紀錄
- DELETE /api/readings/{id} → 刪除紀錄

## 2. 前端：抽牌頁面

### 更新 src/lib/types/index.ts
- 定義 TypeScript 型別，對應後端 DTO

### / 首頁（抽牌頁面）
- 選擇牌陣類型（單張 / 三張 / 凱爾特十字）
- 可選輸入提問文字
- 點擊「抽牌」→ 呼叫 POST /api/readings
- 顯示結果：
  - 每張牌的中文名稱
  - 正位或逆位
  - 牌義解讀
  - 在牌陣中的位置含義
- 結果頁有「再抽一次」和「查看歷史」的連結

## 3. 技術要求
- 後端用 DI 注入 Service
- 前端所有 API 呼叫走 src/lib/api.ts
- 前端要有 loading 狀態和錯誤處理
- UI 先用基本 CSS，不需要動畫效果

## 4. 驗證清單
- Swagger 能測試所有 API 端點
- 前端能完成抽牌流程：選牌陣 → 抽牌 → 看結果
- 資料庫 readings 表有正確的紀錄
- 抽牌結果每次不同
- 未登入時 API 回 401
```

---

## Phase 4：歷史紀錄 + 個人頁面

### Claude Code Prompt — Phase 4

```
接續前面的 tarot-app 專案，請幫我完成 Phase 4：歷史紀錄 + 個人頁面。

## 1. 後端新增 API

### ProfileController.cs
- GET /api/profile → 取得目前用戶的 profile
- PUT /api/profile → 更新 display_name

### StatsController.cs（或加在 ReadingController 裡）
- GET /api/readings/stats → 回傳統計資料：
  - 總抽牌次數
  - 最常抽到的牌 top 5（含次數）
  - 各牌陣使用次數
  - 最近一次抽牌時間

統計盡量在資料庫層用 SQL/LINQ 算好，不要全部撈回記憶體。

## 2. 前端頁面

### /history 歷史紀錄
- 列出所有抽牌紀錄，按時間倒序
- 每筆顯示：日期、牌陣類型、提問（如有）、抽到的牌名稱
- 點擊可展開看完整牌義
- 支援刪除（確認對話框）
- 分頁功能（每頁 10 筆，上一頁/下一頁）
- 空狀態提示：「還沒有抽牌紀錄，去抽一張吧！」

### /profile 個人頁面
- 顯示 email 和顯示名稱
- 可修改顯示名稱（inline edit 或表單）
- 統計資料視覺化（用文字即可，不需要圖表）
- 登出按鈕

### 導航列元件
- 建立 src/lib/components/Navbar.svelte
- 連結：首頁（抽牌）、歷史紀錄、個人頁面
- 顯示目前用戶的 display_name
- 在 +layout.svelte 中引入

## 3. 技術要求
- 分頁用 .NET 的 skip/take 實作
- 前端每個頁面有 loading 和 error 狀態
- 刪除操作要有確認步驟
- TypeScript 型別完整

## 4. 驗證清單
- 歷史紀錄能正確分頁
- 能刪除紀錄，刪除後列表更新
- 能修改顯示名稱
- 統計資料正確
- 導航列在所有頁面正常顯示
```

---

## Phase 5：部署（Zeabur — 前後端統一）✅ 已完成

> **部署平台已由 Cloudflare Pages + Azure 改為 Zeabur**，前後端都在同一個 Zeabur Project，透過內部網路通訊。

### 架構變更

| 項目 | 舊方案 | 新方案 |
|------|--------|--------|
| 前端部署 | Cloudflare Pages（靜態） | Zeabur（Node.js server） |
| 後端部署 | Azure App Service | Zeabur（同 Project） |
| 前後端通訊 | 瀏覽器 → 後端 API（公開網路） | SvelteKit server → 後端 API（Zeabur 內部網路） |
| CORS | 必須設定 | 選配（server-to-server 不需要） |
| API Key 曝露 | JWT token 在瀏覽器 Network tab 可見 | token 只在 server 端，不暴露給瀏覽器 |

### 已完成的程式碼修改

1. **Production Dockerfiles**
   - `backend/Dockerfile`：multi-stage .NET 8 build，port 8080
   - `frontend/Dockerfile`：multi-stage Node.js build，port 3000
   - 原本 dev 用的重新命名為 `Dockerfile.dev`

2. **Server-side API wrapper**
   - 新建 `frontend/src/lib/server/api.ts`
   - 讀取 `INTERNAL_API_URL` env var（server-only），不暴露到瀏覽器
   - Dev：`http://backend:5098`（Docker 內部 DNS）
   - Prod：`http://<service>.zeabur.internal:8080`

3. **頁面改為 server-side 資料載入**
   - `src/routes/+page.server.ts`：draw form action
   - `src/routes/history/+page.server.ts`：load + delete action
   - `src/routes/profile/+page.server.ts`：load + updateName action
   - 3 個 `.svelte` 頁面改用 `$props()` 接收 `data` / `form`，使用 `use:enhance`

4. **Capacitor 備用設定**
   - `frontend/svelte.config.static.js`：adapter-static，供 Phase 6 iOS 打包使用

### 你需要在 Zeabur 手動完成

1. 在 Zeabur Dashboard 建立新 Project
2. 新增兩個 Service，都連結同一個 GitHub repo：
   - **backend** 服務：Root Directory 設為 `backend/`，Zeabur 會自動偵測 `Dockerfile`
   - **frontend** 服務：Root Directory 設為 `frontend/`

3. **後端服務環境變數**：
   ```
   PUBLIC_SUPABASE_URL=https://xxx.supabase.co
   SUPABASE_JWT_SECRET=你的JWT_SECRET
   SUPABASE_DB_CONNECTION_STRING=你的連線字串
   ALLOWED_ORIGINS=https://你的前端.zeabur.app
   ASPNETCORE_ENVIRONMENT=Production
   ```

4. **前端服務環境變數**（build 時需要 PUBLIC_* 變數）：
   ```
   PUBLIC_SUPABASE_URL=https://xxx.supabase.co
   PUBLIC_SUPABASE_ANON_KEY=你的anon_key
   INTERNAL_API_URL=http://backend.zeabur.internal:8080
   ```
   > 注意：`INTERNAL_API_URL` 中的 `backend` 要換成你在 Zeabur 上的後端服務名稱

5. 在 Supabase → Authentication → URL Configuration 加入前端 Zeabur 網域作為 Redirect URL

### 驗證清單

- [ ] `docker compose up --build` 前後端都能啟動（本地驗證）
- [ ] 後端 health check：`https://你的後端.zeabur.app/api/health`
- [ ] 前端能正常打開
- [ ] 登入/註冊流程正常
- [ ] 抽牌功能正常（form action 觸發）
- [ ] 歷史紀錄正常（分頁、刪除）
- [ ] 個人頁面正常（資料顯示、名稱更新）
- [ ] 瀏覽器 Network tab 看不到直接打 .NET API 的請求

---

## Phase 6：Capacitor iOS 打包（後續）

### 你需要先手動完成
- 安裝 Xcode（Mac 限定）
- 有 Apple Developer 帳號（免費帳號可以 sideload 到自己裝置）

### Claude Code Prompt — Phase 6

```
接續前面的 tarot-app 專案，請幫我加入 Capacitor 打包成 iOS App。

## 1. Capacitor 初始化
在 frontend/ 目錄下：
- 安裝 @capacitor/core 和 @capacitor/cli
- npx cap init（App name: 塔羅牌, Package ID: com.yourname.tarot）
- 建立 capacitor.config.ts：
  - webDir: 'build'（SvelteKit adapter-static 的輸出目錄）
  - server.allowNavigation: ['你的Azure App Service URL', '你的Supabase URL']

## 2. iOS 平台
- npx cap add ios
- 設定 Info.plist 必要權限（如果有需要）

## 3. 環境變數處理
- Capacitor 打包時環境變數要在 build 時注入
- 建立一個 build:ios script：
  ```json
  "build:ios": "PUBLIC_API_BASE_URL=https://你的azure.azurewebsites.net pnpm build && npx cap sync ios"
  ```

## 4. Auth 調整
- Supabase Auth 的 redirect URL 要加上 Capacitor 的 custom scheme
- 在 Supabase Dashboard → Authentication → URL Configuration 加入 capacitor://localhost
- 前端 supabase.ts 可能需要根據平台判斷 redirect URL

## 5. UI 調整
- 處理 iOS safe area（status bar、home indicator）
- 在 app.html 加入 viewport-fit=cover meta tag
- CSS 加入 env(safe-area-inset-*) padding

## 6. 建置與測試流程
- pnpm build（產出靜態檔案）
- npx cap sync ios（同步到 iOS 專案）
- npx cap open ios（在 Xcode 開啟）
- 在 Xcode 選擇模擬器或實機執行

## 7. 驗證清單
- iOS 模擬器能正常開啟 app
- 登入/註冊流程在 app 內正常
- 抽牌功能正常
- API 呼叫正常（沒有 CORS 或 HTTPS 問題）
- 歷史紀錄和個人頁面正常
- safe area 顯示正確
```

---

## 注意事項總整理

### 每個 Phase 完成後
- git add + commit + push
- 確認驗證清單全部通過
- 有問題就在同一個 Claude Code 對話中追問修正

### 安全事項
- .env 永遠不要 commit（已在 .gitignore）
- Supabase RLS 一定要開
- .NET API 不要暴露 service_role key
- Production 環境關閉 Swagger 或加上認證

### 常見踩坑
- SvelteKit adapter-static 不能用 +server.ts（server routes）
- .NET 在 Docker 中 hot reload 需要設定 DOTNET_USE_POLLING_FILE_WATCHER=true
- Azure Free tier 閒置 20 分鐘會休眠，第一次請求會慢
- Capacitor 打包前一定要先 pnpm build + cap sync
- Supabase 免費方案的 DB 連線數有限（大約 60），EF Core 要設定 connection pool 上限

### 給 Claude Code 的使用建議
- 一次只丟一個 Phase 的 prompt
- 開始前告知 Claude Code「請先閱讀目前專案結構再開始」
- 如果某個 Phase 太大，可以拆成 Phase 3a、3b 分次執行
- 每個 Phase 結束後做一次完整測試再進入下一個
