# Tarot App

塔羅占卜應用，monorepo 架構：SvelteKit 前端 + ASP.NET Core 8 後端 + Supabase（Auth/Postgres），自架於 OCI ARM（Docker + Nginx，Cloudflare proxy）。

> 反向代理（Nginx 容器）由獨立的 [oci-infra](https://github.com/rider159159/oci-infra) repo 管理。本 app 只 expose 進共用的 `web` Docker network。

線上版：
- 站台：https://tarot.rydercloud.cc
- API：https://tarot.rydercloud.cc/api（與前端同域，由 oci-infra 的 Nginx 反代至後端）

更詳細的架構、API、部署說明見 [CLAUDE.md](./CLAUDE.md)。Observability
基礎與尚未部署的 live checks 見 [docs/observability.md](./docs/observability.md)。

## 本地啟動

### 前置需求
- Docker Desktop（推薦）；或本機安裝 Node.js 22.x、pnpm 9.15.4、.NET 8 SDK
- 一個 Supabase 專案（拿 URL、anon key、JWT secret、DB connection string）

### 1. 設定環境變數

複製 `.env.example` 為 `.env` 並填入 Supabase 設定：

```bash
cp .env.example .env
```

`.env` 必填欄位：
- `PUBLIC_SUPABASE_URL`
- `PUBLIC_SUPABASE_ANON_KEY`
- `SUPABASE_JWT_SECRET`
- `SUPABASE_DB_CONNECTION_STRING`

> `frontend/.env` 是指向根目錄 `.env` 的 symlink，不需要另外維護。
>
> `TEST_USER_PASSWORD` 是給 AI / 煙測用的測試帳號密碼，本機開發可留空。

### 2. 啟動（Docker，推薦）

```bash
docker compose up --build
```

服務位址：
| 服務 | URL | 備註 |
|------|-----|------|
| 前端 | http://localhost:5173 | Vite dev server，hot reload |
| 後端 | http://localhost:5098 | `dotnet watch`，hot reload |
| Swagger | http://localhost:5098/swagger | 後端 API 文件（Dev 環境才有） |
| Health | http://localhost:5098/api/health | 不需要 auth |

第一次跑會下載 image 並裝 dependencies，需要幾分鐘；之後啟動很快。

背景執行加 `-d`：`docker compose up -d --build`，停止用 `docker compose down`。

### 3. 啟動（不用 Docker）

兩個服務分開跑：

```bash
# 終端 1：前端
cd frontend
pnpm install
pnpm dev

# 終端 2：後端
cd backend
dotnet watch run --project TarotApi
```

不用 Docker 時，記得把 `.env` 裡的 `INTERNAL_API_URL` 從 `http://backend:5098` 改成 `http://localhost:5098`。

## 常用指令

```bash
# 前端型別檢查
cd frontend && pnpm check

# 前端 production build（輸出到 frontend/build/）
cd frontend && pnpm build

# 後端 build
cd backend && dotnet build TarotApi
```

## 疑難排解

- **後端啟動失敗**：檢查 `.env` 是否填完四個必填欄位。
- **前端拿不到資料 / 401**：確認 `PUBLIC_SUPABASE_URL` 和後端用的同一個 Supabase 專案。
- **CORS 錯誤**：把前端 origin 加進 `.env` 的 `ALLOWED_ORIGINS`（逗號分隔，不含尾斜線）。
- **Docker hot reload 沒反應**：Vite 已啟用 polling、`dotnet watch` 有設 `DOTNET_USE_POLLING_FILE_WATCHER=true`，正常情況下能用；若仍失效重啟容器。

更多細節（資料庫遷移、Zeabur 部署、Supabase JWKS 設定）見 [CLAUDE.md](./CLAUDE.md)。
