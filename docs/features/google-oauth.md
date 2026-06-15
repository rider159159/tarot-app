# 功能名稱：Google OAuth 登入

> Track A（新功能） · 需求已釐清 · 分析日期 2026-06-15

## User Story

作為一個想快速開始占卜的使用者，我希望能用 Google 帳號一鍵登入／註冊，以便省去填
email、設密碼、收驗證信的流程，降低進入門檻。

## Acceptance Criteria

- [ ] `/login` 與 `/register` 都有「使用 Google 登入」按鈕
- [ ] 點按後導向 Google 授權頁，授權完成後回到 app 並完成登入
- [ ] 首次用 Google 登入會自動建立帳號與 profile（display_name 取 Google email 的
      `@` 前段，沿用現況）
- [ ] 登入後正確導回原本要去的頁面（沿用既有 `next` / `returnTo` 行為）
- [ ] 在正式網域 `https://tarot.rydercloud.cc` 與本地 `http://localhost:5173`
      都能完整跑完 OAuth（redirect URI 都要登錄）
- [ ] OAuth 換 session 失敗時，導回 `/login` 並顯示中性錯誤訊息（不可沿用
      「信箱已驗證」那段誤導文案）
- [ ] 後端 `/api/*` 能用 Google 簽發的 JWT 通過驗證（預期免改，需驗證）

## 技術影響

### 前端

- `frontend/src/lib/supabase.ts` 之外新增共用 helper，呼叫
  `supabase.auth.signInWithOAuth({ provider: 'google', options: { redirectTo } })`，
  `redirectTo` 指向 `/auth/callback?next=<returnTo>`。
- `frontend/src/routes/login/+page.svelte`、`frontend/src/routes/register/+page.svelte`：
  加 Google 按鈕（建議抽成 `frontend/src/lib/components/GoogleSignInButton.svelte`
  共用元件，吃 `returnTo` prop）。
- `frontend/src/routes/auth/callback/+page.svelte`：目前 `exchangeCodeForSession`
  失敗一律導向 `/login?notice=email_verified`，需區分 OAuth 失敗情境給中性錯誤
  （在 `login/+page.server.ts` 的 `LOAD_MESSAGES` 加一個 key，如 `oauth_failed`）。
  注意：OAuth 與既有 email 驗證走的是同一條 PKCE callback（`code` → `exchangeCodeForSession`），
  code_verifier 同樣存在發起登入的瀏覽器，因此 callback 主流程可直接複用。
- `frontend/src/hooks.server.ts`：`/auth/callback` 已在 `PUBLIC_PATHS`，免改。

### 後端

預期 **零改動**：

- JWKS 驗證不分 provider，Google 簽發的 Supabase JWT（audience `authenticated`）
  一樣通過。
- profile 缺漏時 `ProfileService.GetProfile` 已會自我修復 backfill。

列為「需驗證」而非「需開發」——實作後用測試流程確認一次。

### DB

**無 migration**。`handle_new_user()` trigger（migration 002）對 OAuth 註冊一樣會
在 `auth.users` INSERT 時觸發，display_name 取 email `@` 前段。

### 第三方設定（本功能主要工作量在這）

正式網域：`https://tarot.rydercloud.cc`（OCI VPS 主機 + Cloudflare 代理／DNS）。

1. **Google Cloud Console** — 建立 OAuth 2.0 Client ID
   - OAuth 同意畫面：app 名稱、scope `email`、`profile`
   - Authorized redirect URI：`{PUBLIC_SUPABASE_URL}/auth/v1/callback`
     （這是 Supabase 的固定 callback，不是 app 的）
   - Authorized JavaScript origins：
     - `https://tarot.rydercloud.cc`
     - `http://localhost:5173`（本地開發）

2. **Supabase Dashboard → Authentication → Providers → Google**
   - 填入 Google 的 Client ID / Client Secret，啟用 provider

3. **Supabase Dashboard → Authentication → URL Configuration**
   - Site URL：`https://tarot.rydercloud.cc`
   - Redirect URLs 白名單加入：
     - `https://tarot.rydercloud.cc/auth/callback`
     - `http://localhost:5173/auth/callback`

4. **Cloudflare**
   - 確認 `tarot.rydercloud.cc` 走 HTTPS（SSL/TLS 模式至少 Full）。app 以 https
     對外時，`hooks.server.ts` 會自動把 session cookie 設為 Secure（見該檔註解）。
   - 確認 Cloudflare 不要快取 `/auth/callback`（帶 `?code=` 的回跳，應為動態）。

## 優先級評估

- **使用者價值：中高** — 直接降低註冊摩擦，對「新手好奇」型使用者轉化幫助明顯。
- **開發成本：S** — 程式碼改動小（一個按鈕元件 + callback 文案微調）；主要成本在
  雲端設定與跨網域 redirect URI 驗證。
- **學習收益**：OAuth 2.0 / PKCE 流程、Supabase 第三方 provider 串接、redirect URI
  白名單與 open-redirect 防護、Cloudflare 前置代理對 cookie/HTTPS 的影響。
- **建議順位**：可優先處理，成本低且對成長有幫助。

## 決策紀錄

- 按鈕位置：`/login` + `/register` 都放（一致體驗）。
- 帳號連結：沿用 Supabase 預設行為（不另做自訂連結邏輯）。
- 顯示名稱／頭像：沿用現況，display_name 取 email `@` 前段；本期不帶入 Google
  full_name / avatar。

## 待釐清

- **帳號連結**沿用 Supabase 預設，但預設行為依專案設定而定 → 實作前由你在
  Supabase Dashboard 確認該專案目前的 identity linking 設定一次。
- **zeabur 部署是否仍在線**：CLAUDE.md 仍記載 `rtarot.zeabur.app`。若該站還對外
  服務、也要支援 Google 登入，需把它的 origin / redirect 一併加入白名單；若已
  汰換為 OCI VPS，請順手更新 CLAUDE.md 的部署表 → 需要你確認。
