# 塔羅牌 Web App — Phase 0 ~ 1 技術總覽

> 目標讀者：有 Vue + Node.js 後端經驗的開發工程師
> 涵蓋範圍：Phase 0（開發環境 + 專案初始化）與 Phase 1（Supabase 資料表 + 前端會員系統）

---

## 1. 整體架構

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│   SvelteKit     │─────▶│  .NET Web API    │─────▶│   Supabase      │
│   (前端 SPA)    │      │  (後端 API)       │      │  (DB + Auth)    │
│  Cloudflare     │      │  Azure App       │      │  supabase.co    │
│  Pages          │      │  Service         │      │                 │
└─────────────────┘      └──────────────────┘      └─────────────────┘
```

**分工原則：**
- 前端直接用 Supabase Auth 做登入 / 註冊（不經過 .NET）
- 前端拿到 JWT token 後，打 .NET API 帶 `Authorization: Bearer <token>`
- .NET API 驗證 Supabase JWT 後執行商業邏輯

---

## 2. 技術棧速查

| 項目 | 選擇 | Vue 生態對照 |
|------|------|-------------|
| 前端框架 | **SvelteKit 2 + Svelte 5** | 類似 Nuxt 3 + Vue 3 |
| 前端建構 | **Vite 6** | 跟 Vue 一樣用 Vite |
| 靜態部署 | **adapter-static**（SPA mode） | 類似 Nuxt `ssr: false` + `generate` |
| 後端 API | **ASP.NET Core 8**（Controller 風格） | 替代 Express / Koa |
| 認證系統 | **Supabase Auth** | 類似 Firebase Auth |
| 資料庫 | **Supabase PostgreSQL** + RLS | 類似 Firebase + Firestore Rules |
| API 文件 | **Swagger / OpenAPI**（Swashbuckle） | 類似 swagger-jsdoc |
| 容器化 | **Docker Compose**（前端 + 後端） | 同 |
| 套件管理 | **pnpm**（前端）/ **NuGet**（後端） | npm/yarn → pnpm |

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
├── frontend/                 # SvelteKit SPA
│   ├── Dockerfile            # Node 22, pnpm
│   ├── svelte.config.js      # adapter-static (fallback: 200.html)
│   ├── vite.config.ts        # Docker HMR 設定
│   └── src/
│       ├── routes/           # 頁面路由（類似 Nuxt pages/）
│       │   ├── +layout.js    # export const ssr = false
│       │   ├── +layout.svelte# Auth guard + 全域 layout
│       │   ├── +page.svelte  # 首頁（塔羅抽牌）
│       │   ├── login/        # 登入頁
│       │   └── register/     # 註冊頁
│       └── lib/
│           ├── supabase.ts   # Supabase client 初始化
│           ├── api.ts        # .NET API fetch wrapper（自動帶 JWT）
│           ├── stores/
│           │   └── auth.svelte.ts  # Auth 狀態管理（Svelte 5 runes）
│           ├── tarot/        # 塔羅牌核心資料 & 邏輯
│           ├── components/   # UI 元件
│           └── types/        # TypeScript 型別定義
├── backend/                  # ASP.NET Core 8
│   ├── Dockerfile            # .NET 8 SDK, dotnet watch
│   └── TarotApi/
│       ├── Program.cs        # CORS + Swagger + Controller 註冊
│       └── Controllers/
│           └── HealthController.cs  # GET /api/health
└── supabase/
    └── migrations/
        ├── 001_create_readings.sql    # (Phase 0, 已被 002 取代)
        └── 002_auth_tables.sql        # profiles + readings + RLS
```

---

## 5. Phase 0 — 做了什麼

### 5.1 Docker Compose 開發環境

一個 `docker compose up --build` 同時啟動前後端，支援 hot reload：

```yaml
services:
  frontend:            # Node 22 + pnpm dev
    ports: 5173:5173
    volumes: 掛載原始碼 + .env (readonly)
  backend:             # .NET 8 SDK + dotnet watch
    ports: 5098:5098
    volumes: 掛載原始碼（排除 bin/obj）
```

**與 Node.js 開發的差異：**
- 後端使用 `dotnet watch` 做 hot reload（對應 `nodemon`）
- 需要排除 `bin/` 和 `obj/`（對應 `node_modules`）
- Vite HMR 在 Docker 裡需要 `usePolling: true`（因為 Linux 容器不支援 macOS 的 FSEvents）

### 5.2 前端 SvelteKit 初始化

- **adapter-static**：編譯成純靜態檔，`fallback: '200.html'` 讓 client-side routing 正常運作（類似 Vue 的 `historyApiFallback`）
- **`ssr = false`**：整個 app 是 client-side rendering，跟 Vue SPA 行為一致
- **Supabase client**：env vars 為空時 graceful 回傳 null，不會爆掉

### 5.3 後端 ASP.NET Core 初始化

```csharp
// Program.cs — 類比 Express 的 app.js
builder.Services.AddControllers();          // 註冊 Controller（類似 Express Router）
builder.Services.AddSwaggerGen();           // 自動生成 API 文件
builder.Services.AddCors(...);              // CORS 白名單（localhost:5173）

app.UseCors("AllowFrontend");              // 類似 cors({ origin: ... })
app.MapControllers();                       // 類似 app.use('/api', router)
```

**對照 Express：**

| ASP.NET Core | Express.js |
|---|---|
| `AddControllers()` + `MapControllers()` | `app.use('/api', router)` |
| `[Route("api/[controller]")]` | `router.get('/health', ...)` |
| `AddSwaggerGen()` | `swagger-jsdoc` + `swagger-ui-express` |
| `AddCors()` | `cors({ origin: ... })` |
| `dotnet watch` | `nodemon` |

### 5.4 前端 API Wrapper

`api.ts` 封裝了所有打後端的 HTTP 請求，自動帶 JWT：

```typescript
// 自動從 Supabase session 拿 token 加到 header
async function getAuthHeaders() {
  const { data } = await supabase.auth.getSession();
  headers['Authorization'] = `Bearer ${data.session.access_token}`;
}

// CRUD 方法（類似 axios instance）
export async function apiGet<T>(path: string): Promise<T> { ... }
export async function apiPost<T>(path: string, body: unknown): Promise<T> { ... }
export async function apiPut<T>(path: string, body: unknown): Promise<T> { ... }
export async function apiDelete(path: string): Promise<void> { ... }
```

---

## 6. Phase 1 — 做了什麼

### 6.1 Supabase 資料表設計

在 `supabase/migrations/002_auth_tables.sql` 中建立兩張表：

#### profiles 表
```sql
CREATE TABLE profiles (
  id           UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  display_name TEXT NOT NULL,
  created_at   TIMESTAMPTZ DEFAULT now(),
  updated_at   TIMESTAMPTZ DEFAULT now()
);
-- RLS: 只能查詢/更新自己的 profile
-- Trigger: 註冊時自動建立 profile（取 email @ 前面當 display_name）
```

#### readings 表（塔羅抽牌紀錄）
```sql
CREATE TABLE readings (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id        UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  spread_type    TEXT NOT NULL CHECK (...),  -- 'single' | 'three-card' | 'celtic-cross'
  question       TEXT,
  cards          JSONB NOT NULL,             -- 抽到的牌（陣列，含正逆位）
  interpretation TEXT,
  notes          TEXT,
  created_at     TIMESTAMPTZ DEFAULT now()
);
-- RLS: CRUD 全部限制 auth.uid() = user_id
-- Index: (user_id, created_at DESC) 加速查詢
```

**RLS（Row Level Security）簡介：**
- 類似 Firestore Security Rules，在資料庫層做權限控管
- `auth.uid()` 從 JWT 自動解析當前用戶 ID
- 即使前端被 hack，也無法存取其他用戶的資料

### 6.2 前端會員系統

#### Auth Store（`auth.svelte.ts`）

用 Svelte 5 runes 管理全域 auth 狀態（類比 Pinia store）：

```typescript
// Svelte 5 module-level reactive state（類似 Pinia 的 defineStore）
let user = $state(null);       // 類似 ref(null)
let session = $state(null);
let loading = $state(true);

// 監聽 Supabase auth 狀態變化（類似 Firebase onAuthStateChanged）
supabase.auth.onAuthStateChange((_event, newSession) => {
  session = newSession;
  user = newSession?.user ?? null;
});

// 匯出 reactive getter（類似 Pinia 的 storeToRefs）
export const auth = {
  get user() { return user; },
  login, register, logout
};
```

#### 頁面路由 & Auth Guard

```
routes/
├── +layout.svelte    → 未登入自動導向 /login（類似 Vue Router 的 beforeEach）
├── login/+page.svelte
└── register/+page.svelte
```

---

## 7. 環境變數

```env
# Supabase（前端用 PUBLIC_ 開頭才能在 client 端存取）
PUBLIC_SUPABASE_URL=https://xxx.supabase.co
PUBLIC_SUPABASE_ANON_KEY=eyJ...

# Supabase（後端用）
SUPABASE_JWT_SECRET=your-jwt-secret
SUPABASE_DB_CONNECTION_STRING=postgresql://...

# API
PUBLIC_API_BASE_URL=http://localhost:5098
```

> SvelteKit 的 `PUBLIC_` 前綴 ≈ Vue/Vite 的 `VITE_` 前綴

---

## 8. 開發指令

```bash
# 啟動全部（Docker）
docker compose up --build

# 或分開跑
cd frontend && pnpm dev          # http://localhost:5173
cd backend/TarotApi && dotnet watch run  # http://localhost:5098

# TypeScript 檢查
cd frontend && pnpm check

# 打包前端
cd frontend && pnpm build        # 輸出到 build/
```

---

## 9. 驗證清單（Phase 0 + 1 完成後應能）

- [x] `docker compose up` 前後端都啟動
- [x] http://localhost:5173 看到前端頁面
- [x] http://localhost:5098/api/health 回傳 `{ status: "ok", timestamp: "..." }`
- [x] http://localhost:5098/swagger 看到 Swagger UI
- [x] 修改 `.svelte` 檔 → 頁面自動更新（HMR）
- [x] 修改 `.cs` 檔 → API 自動重新編譯（dotnet watch）
- [x] 可以註冊新帳號（Supabase Auth）
- [x] 可以登入 / 登出
- [x] 未登入時自動導向登入頁

---

## 10. 接下來（Phase 2+）

| Phase | 內容 |
|-------|------|
| Phase 2 | .NET 後端 API — JWT 驗證中介層、Supabase DB 串接 |
| Phase 3 | 塔羅牌核心 — 78 張牌資料、3 種牌陣、抽牌演算法 |
| Phase 4 | 歷史紀錄 + 個人頁面 |
| Phase 5 | 部署（Cloudflare Pages + Azure App Service） |
| Phase 6 | Capacitor iOS 打包 |
