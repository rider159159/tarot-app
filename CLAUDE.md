# Tarot App — Claude 開發脈絡

## 總覽
單一 repo（monorepo），包含 SvelteKit 前端與 .NET 8 後端，部署於 Zeabur。

## 架構

```
tarot-app/
├── frontend/              SvelteKit 2 + Svelte 5 (adapter-node, ssr=false)
│   ├── src/
│   │   ├── hooks.server.ts       認證 middleware 與路由守衛
│   │   ├── routes/               頁面（見下方「前端路由」）
│   │   └── lib/
│   │       ├── supabase.ts       Supabase 瀏覽器端 client
│   │       ├── server/api.ts     伺服器端 API client
│   │       ├── components/       UI 元件（6 個檔案）
│   │       ├── tarot/            牌卡資料、牌陣、圖片
│   │       ├── types/            TypeScript 型別定義
│   │       └── utils/            占卜相關 helper
│   └── svelte.config.js          adapter-node，建置輸出至 build/
├── backend/               ASP.NET Core 8 + EF Core + Npgsql
│   └── TarotApi/
│       ├── Program.cs            DI、CORS、JWT、middleware pipeline
│       ├── Controllers/          4 個 controller（Health、Tarot、Reading、Profile）
│       ├── Services/             TarotService、ReadingService、ProfileService
│       ├── Models/               entity、DTO、enum
│       ├── Data/                 EF Core context、78 張牌種子資料
│       └── Middleware/           ExceptionHandlingMiddleware
├── database/              Supabase migration
├── docker-compose.yml     僅供本地開發
└── .env                   共用環境變數（frontend/.env 為其符號連結）
```

## Zeabur 部署
| 服務 | 名稱 | 公開 URL | Port |
|---------|------|-----------|------|
| 前端 | tarot-app-uram | rtarot.zeabur.app | 8080 |
| 後端 | tarot-app-ist | rtarot-api.zeabur.app | 8080 |

健康檢查：`GET https://rtarot-api.zeabur.app/api/health`

## 本地開發

前置需求：Node.js 22.x（依 `.nvmrc`）、pnpm 9.15.4、.NET 8 SDK

```bash
# 方式 1：Docker（建議，會同時啟動兩個服務）
docker compose up --build
# 前端 → http://localhost:5173  (Vite dev server，hot reload)
# 後端 → http://localhost:5098  (dotnet watch，live reload)
# Swagger → http://localhost:5098/swagger
```

```bash
# 方式 2：個別啟動
cd frontend && pnpm install && pnpm dev
cd backend && dotnet watch run --project TarotApi
```

常用指令：
```bash
cd frontend && pnpm check    # TypeScript 檢查
cd frontend && pnpm build    # production 建置 → build/，含 200.html SPA fallback
```

## 環境變數

### 前端（build-time，由 Vite 在建置時內嵌）
```
PUBLIC_SUPABASE_URL=
PUBLIC_SUPABASE_ANON_KEY=
```

### 前端（伺服器端，runtime）
```
INTERNAL_API_URL=            # 伺服器端呼叫 API 用的後端 URL（預設：http://localhost:5098）
```

### 後端（runtime）
```
PUBLIC_SUPABASE_URL=          # 同時用於 JWKS endpoint
SUPABASE_JWT_SECRET=          # 啟動時必須提供
SUPABASE_DB_CONNECTION_STRING= # PostgreSQL 連線字串
ALLOWED_ORIGINS=              # 以逗號分隔（例如 https://rtarot.zeabur.app）
```

`.env` 位於 repo 根目錄；`frontend/.env` 是指向 `../.env` 的符號連結。

## 前端路由

| 路由 | 頁面 | 說明 |
|-------|------|-------------|
| `/` | 首頁 | 塔羅占卜（選牌陣、輸入問題、抽牌） |
| `/login` | 登入 | Email／密碼登入 |
| `/register` | 註冊 | 新帳號註冊 |
| `/history` | 歷史紀錄 | 分頁的占卜歷史（PAGE_SIZE=10），可刪除 |
| `/profile` | 個人資料 | 使用者資訊、占卜統計、修改名稱 |
| `/auth/callback` | — | OAuth callback（用 code 換 session） |
| `/auth/logout` | — | POST endpoint，登出後導向 /login |

認證路由規則：未登入使用者導向 `/login`；已登入使用者從 `/login`、`/register` 導離。

## 後端 API endpoint
除健康檢查外，全部需要 `Authorization: Bearer <supabase-jwt>`。

| Method | 路徑 | 需認證 | 說明 |
|--------|------|------|-------------|
| GET | /api/health | 否 | 健康檢查 |
| GET | /api/tarot/cards | 是 | 列出全部 78 張牌 |
| GET | /api/tarot/cards/{id} | 是 | 牌卡細節 |
| GET | /api/readings | 是 | 列出使用者的占卜紀錄（query：`page`、`pageSize`，上限 50） |
| GET | /api/readings/{id} | 是 | 取得指定占卜紀錄 |
| GET | /api/readings/stats | 是 | 占卜統計（常出現的牌、牌陣使用次數） |
| POST | /api/readings | 是 | 建立占卜紀錄 |
| DELETE | /api/readings/{id} | 是 | 刪除占卜紀錄 |
| GET | /api/profile | 是 | 取得個人資料 |
| PUT | /api/profile | 是 | 更新個人資料（displayName） |

Swagger UI 僅在 Development 環境的 `/swagger` 提供。

## 認證
- 前端的 Supabase Auth 簽發 JWT（ES256）
- 後端透過 JWKS 驗證：`{PUBLIC_SUPABASE_URL}/auth/v1/.well-known/openid-configuration`
- `SUPABASE_JWT_SECRET` 會被載入，但實際的簽章金鑰來源是 JWKS
- 所有 controller 套用全域 `[Authorize]` filter；僅健康檢查掛 `[AllowAnonymous]`

### 測試帳號（供 AI／smoke test 使用）
有一個預先建立的 Supabase 使用者，用來打需認證的 endpoint 而不必每次重新註冊。Email 已提交於 `.env.example`；密碼僅存在本地 `.env`（已 gitignore）。Email 已驗證，可直接用 password grant。

當 Claude 需要 Bearer token 來呼叫 `/api/readings`、`/api/profile` 等時，載入 `.env` 並用測試帳密換 `access_token`：

```bash
set -a && source .env && set +a
TOKEN=$(curl -s -X POST "$PUBLIC_SUPABASE_URL/auth/v1/token?grant_type=password" \
  -H "apikey: $PUBLIC_SUPABASE_ANON_KEY" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$TEST_USER_EMAIL\",\"password\":\"$TEST_USER_PASSWORD\"}" | jq -r .access_token)

curl -H "Authorization: Bearer $TOKEN" http://localhost:5098/api/profile
```

若本地缺少 `TEST_USER_PASSWORD`，請詢問使用者——不要嘗試註冊新帳號或重設密碼。

## 關鍵原始碼檔案

### 後端
- `backend/TarotApi/Program.cs` — DI 接線、CORS、JWT 設定、middleware pipeline
- `backend/TarotApi/Controllers/` — HealthController、TarotController、ReadingController、ProfileController
- `backend/TarotApi/Services/TarotService.cs` — 抽牌（Fisher-Yates 洗牌、加密級 RNG）、牌陣設定、靈感牌邏輯
- `backend/TarotApi/Services/ReadingService.cs` — 占卜紀錄 CRUD、統計彙整（JSONB 用 raw SQL）
- `backend/TarotApi/Services/ProfileService.cs` — 個人資料 CRUD
- `backend/TarotApi/Data/TarotDbContext.cs` — EF Core context（profiles、readings 表）
- `backend/TarotApi/Data/TarotCards.cs` — 78 張牌靜態資料（中文牌名、牌義、關鍵字）
- `backend/TarotApi/Models/` — Profile、Reading entity；SpreadType enum；8 個 DTO
- `backend/TarotApi/Middleware/ExceptionHandlingMiddleware.cs` — 全域錯誤處理
- `backend/TarotApi/Extensions/ClaimsPrincipalExtensions.cs` — 從 JWT 的 sub claim 取得 GetUserId()

### 前端
- `frontend/src/hooks.server.ts` — 認證 middleware、session 驗證、路由守衛
- `frontend/src/lib/supabase.ts` — Supabase 瀏覽器端 client（@supabase/ssr）
- `frontend/src/lib/server/api.ts` — 伺服器端 API client（createServerApiClient，帶 Bearer token）
- `frontend/src/lib/types/index.ts` — 全部 TypeScript 型別（TarotCard、SpreadType、DTO、API 回應型別）
- `frontend/src/lib/utils/reading.ts` — mapApiResponse、getSpreadName、formatDate 等 helper
- `frontend/src/lib/tarot/cards.ts` — 整合的 78 張牌資料（allCards、cardById、getCardById）
- `frontend/src/lib/tarot/major-arcana.ts` — 22 張大牌定義
- `frontend/src/lib/tarot/minor-arcana.ts` — 56 張小牌定義
- `frontend/src/lib/tarot/spread.ts` — 5 種牌陣設定（single、three-card-time、three-card-problem、three-card-linear、celtic-cross）
- `frontend/src/lib/tarot/card-images.ts` — 全部 78 張牌的 Wikimedia Commons 圖片 URL
- `frontend/src/lib/tarot/readings.ts` — saveReading()，存入 Supabase
- `frontend/src/lib/components/Navbar.svelte` — 頂部導覽列，依認證狀態顯示
- `frontend/src/lib/components/SpreadSelector.svelte` — 牌陣類型 radio 選擇器（5 個選項）
- `frontend/src/lib/components/QuestionInput.svelte` — 選填的問題 textarea
- `frontend/src/lib/components/DrawButton.svelte` — 抽牌按鈕，含 loading 狀態
- `frontend/src/lib/components/CardResult.svelte` — 單張牌顯示，含圖片、牌義、正逆位
- `frontend/src/lib/components/ReadingDisplay.svelte` — 完整占卜結果，含靈感牌區塊

## 牌陣類型
| Key | 張數 | 說明 |
|-----|-------|-------------|
| `single` | 1 | 每日指引 |
| `three-card-time` | 3（+靈感牌） | 過去／現在／未來 |
| `three-card-problem` | 3（+靈感牌） | 問題／原因／解法 |
| `three-card-linear` | 3（+靈感牌） | 第一／第二／第三 |
| `celtic-cross` | 10（+靈感牌） | 經典 10 張牌陣 |

非單張牌陣會額外多抽一張「靈感牌」，補充整體脈絡。

## 自訂指令

四個「員工」角色，組成 需求分析 → 實作 → 審查 的接力流程；`/mentor` 為獨立的學習輔助。

| 指令 | 角色 | 說明 |
|---------|------|-------------|
| `/feature` | 需求分析師 | 判斷 track（A 新功能／B 修改／C 除錯），把需求整理成可執行 brief，存到 `docs/features/`、`docs/changes/`、`docs/bugs/` |
| `/build` | 開發工程師 | 依 brief 動 code，遵守動工順序（migration→後端→前端）與五條開發紀律 |
| `/qa` | 品質保證 | 啟動乾淨 context 的 `qa-reviewer` sub-agent，依八大清單審查未提交變更（前後端型別一致性、API 合約、UI 狀態、認證、DB 遷移、牌陣邏輯、部署影響、文件更新） |
| `/mentor` | 技術導師 | 解釋程式碼、原理、設計取捨，幫你建立可遷移的心智模型 |

各指令均可帶說明做聚焦（如 `/qa 新增了 weekly-fortune 牌陣`、`/feature 想加每日提醒`）。`qa-reviewer` sub-agent 定義於 `.claude/agents/qa-reviewer.md`。

## 實作順序

收到已分析完成的需求（例如 `/feature` 規格），且沒有其他順序指示時，依下列順序實作：

1. 資料庫遷移 — 寫好 migration 檔，並實際套用到資料庫
2. 後端 — model、service、controller、驗證
3. 前端 — 型別、元件、頁面

理由：schema 是基礎；後端依賴 schema；前端依賴後端 API。migration 檔只有在實際對資料庫執行後才會生效——只部署了會寫入新欄位／新值的後端程式碼、卻沒套用對應 migration，會在 runtime 觸發約束違規（見 custom-spread 的 500 事件）。

## 常見除錯提示

### 後端啟動失敗
確認所有必要環境變數都已設定：`PUBLIC_SUPABASE_URL`、`SUPABASE_JWT_SECRET`、`SUPABASE_DB_CONNECTION_STRING`

### API 回傳 401 Unauthorized
- 確認前端有送 `Authorization: Bearer <token>` header
- token 必須來自 Supabase Auth（ES256，audience：`authenticated`）
- 確認 `PUBLIC_SUPABASE_URL` 與簽發 token 的 Supabase 專案一致

### CORS 錯誤
- 把前端 origin 加進後端的 `ALLOWED_ORIGINS` 環境變數
- 格式：`https://rtarot.zeabur.app`（結尾不加斜線）

### 前端在 Zeabur 建置失敗
- `PUBLIC_SUPABASE_URL` 與 `PUBLIC_SUPABASE_ANON_KEY` 必須在 Zeabur 設為 build-time 環境變數（ARG）

### 資料庫遷移
- migration 位於 `database/`，目標是 Supabase PostgreSQL 實例
- EF Core 在 runtime 使用 `SUPABASE_DB_CONNECTION_STRING`

### Docker hot reload 失效
- 前端：`vite.config.ts` 已啟用 Vite polling（Docker volume 需要）
- 後端：docker-compose.yml 已設定 `DOTNET_USE_POLLING_FILE_WATCHER=true`
