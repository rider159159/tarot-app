# 功能規格：登入區塊補強（Auth Improvements）

> 範圍：補齊現有登入流程的三個缺漏。不含 OAuth 第三方登入。
> 三個區塊彼此獨立，可分批實作；建議實作順序：區塊 C → B → A。

## 背景

現況登入區塊已實作：Email/密碼登入、註冊 + 寄驗證信、Email 驗證回呼、登出、`returnTo` 導流與 route guard。

主要使用者輪廓為「匿名好奇用戶 + 部分轉為回訪用戶」並存。痛點來自回訪這一端：

- 回訪用戶忘記密碼時，App 沒有任何重設入口 — 只能棄帳號重抽匿名牌，過往占卜歷史全部失聯。
- 新註冊用戶沒收到驗證信時無補救入口，帳號卡在未啟用狀態。
- `/auth/callback` 驗證失敗會帶 `?error=email_verification_failed` 跳回 `/login`，但登入頁完全沒讀取這個參數，使用者看不到任何錯誤說明。

成功指標（整體）：
- 忘記密碼流程完成率（進入 `/forgot-password` → 成功重設並登入的比例）
- 驗證信重寄後的帳號啟用率

---

## 區塊 A：忘記密碼 / 重設密碼

### User Story

作為一個忘記密碼的回訪用戶，我希望能透過 Email 收到重設連結並設定新密碼，以便重新登入並找回我的占卜歷史，而不必棄帳號。

### Acceptance Criteria

**請求重設（`/forgot-password`）**
- [ ] 登入頁出現「忘記密碼？」連結，導向 `/forgot-password`
- [ ] `/forgot-password` 提供 Email 輸入欄與送出按鈕
- [ ] 送出後呼叫 `supabase.auth.resetPasswordForEmail(email, { redirectTo })`
- [ ] 不論該 Email 是否存在，都顯示相同的中性訊息（例：「若此信箱已註冊，我們已寄出重設連結」）— 避免帳號列舉
- [ ] Email 格式不合法時前端擋下並提示
- [ ] 已登入用戶造訪 `/forgot-password` 一律重導回 `/`（與 `/login` 行為一致）

**設定新密碼（`/auth/reset-password`）**
- [ ] 重設信連結帶 code 導向 `/auth/reset-password`，頁面用 `exchangeCodeForSession` 建立臨時 session
- [ ] code 無效或過期時顯示錯誤並提供「重新申請重設」連結，不顯示密碼表單
- [ ] 提供新密碼欄與「確認新密碼」欄，兩者需一致
- [ ] 新密碼至少 8 字元（與註冊頁規則一致）
- [ ] 送出後呼叫 `supabase.auth.updateUser({ password })`
- [ ] 重設成功後導向 `/login`（或 `/`），並提示「密碼已更新，請重新登入」
- [ ] 重設成功後既有的其他 session 失效（依賴 Supabase 預設行為，需驗證）

### 技術影響

- **前端**：
  - 新 route `/forgot-password`（`+page.svelte` + `+page.server.ts`）
  - 新 route `/auth/reset-password`（`+page.svelte` + `+page.server.ts`，或 `+server.ts` 處理 code 交換後再渲染表單）
  - `login/+page.svelte` 新增「忘記密碼？」連結
  - `hooks.server.ts` 的 `PUBLIC_PATHS` 加入 `/forgot-password` 與 `/auth/reset-password`
- **後端**：無。密碼重設完全走 Supabase Auth，不經過 .NET API
- **DB**：無 migration
- **第三方**：Supabase Auth 內建 `resetPasswordForEmail`。Supabase 後台需設定 redirect URL 白名單，加入 `{origin}/auth/reset-password`（production 與 localhost 各一）；信件模板可選擇性中文化

### 優先級評估

- 使用者價值：**高** — 對回訪用戶是「能否找回帳號」的存亡問題；目前完全無解
- 開發成本：**M** — 兩個新頁面 + 兩段流程，但 Supabase API 現成，無後端與 DB 改動
- 學習收益：Supabase 雙階段流程（`resetPasswordForEmail` → `exchangeCodeForSession` → `updateUser`）、redirect URL 白名單機制、帳號列舉的防禦設計
- 建議順位：登入補強三項中價值最高，建議排在區塊 B/C 之後實作（因 B/C 成本極低、可快速清掉），但屬同一批次

---

## 區塊 B：重新發送驗證信

### User Story

作為一個註冊後沒收到驗證信的用戶，我希望能重新觸發一次驗證信寄送，以便完成帳號啟用，而不必重新註冊。

### Acceptance Criteria

- [ ] 註冊成功頁（`form?.success` 區塊）新增「沒收到信？重新發送」按鈕
- [ ] 登入頁偵測到「Email 尚未驗證」類錯誤時，顯示「重新發送驗證信」入口
- [ ] 點擊後呼叫 `supabase.auth.resend({ type: 'signup', email })`
- [ ] 重寄需有冷卻時間（前端按鈕 disable 倒數，例 60 秒），避免連點
- [ ] 重寄成功顯示確認訊息；失敗（含 Supabase rate limit）顯示可理解的錯誤
- [ ] 不論 Email 是否存在/是否已驗證，回應訊息保持中性，避免帳號列舉

### 技術影響

- **前端**：
  - `register/+page.svelte` 成功區塊加重寄按鈕；`register/+page.server.ts` 加一個具名 action（如 `?/resend`）或前端直接呼叫 browser client
  - `login/+page.svelte` 在特定錯誤情境顯示重寄入口
  - 需保留 email 值供重寄使用（註冊頁已有 `form.email`）
- **後端**：無
- **DB**：無 migration
- **第三方**：Supabase Auth `resend` API。注意 Supabase 對寄信有預設 rate limit，重寄過快會回錯誤，需在 UI 處理

### 優先級評估

- 使用者價值：**中** — 影響「沒收到信」的註冊用戶子集；多數人首封信就收到，但卡住的人完全無路可走
- 開發成本：**S** — 無新頁面，僅在既有兩頁加按鈕 + 一個 API 呼叫 + 冷卻倒數
- 學習收益：`supabase.auth.resend` 用法、前端冷卻倒數的狀態管理（Svelte 5 runes）
- 建議順位：成本低、可與區塊 C 一起當作快速批次先清掉

---

## 區塊 C：修掉驗證錯誤未顯示

### User Story

作為一個點了過期或無效驗證連結的用戶，我希望登入頁能明確告訴我「驗證失敗」，以便知道下一步該做什麼，而不是面對一個沒有任何提示的登入畫面。

### Acceptance Criteria

- [ ] `login/+page.server.ts` 的 `load` 讀取 `url.searchParams.get('error')`
- [ ] `error=email_verification_failed` 時，回傳對應的中文訊息給頁面
- [ ] `login/+page.svelte` 顯示該訊息（沿用既有 `.error` 樣式，或新增提示樣式）
- [ ] 未知的 `error` 值不顯示原始字串，僅顯示通用錯誤或忽略，避免反射型內容注入
- [ ] 若區塊 B 已完成，此錯誤訊息一併帶出「重新發送驗證信」入口

### 技術影響

- **前端**：
  - `login/+page.server.ts` `load` 新增 `error` 參數解析，回傳給頁面
  - `login/+page.svelte` 顯示 load 來的錯誤訊息（注意與 `form?.error` 區分：一個來自 load、一個來自 action）
- **後端**：無
- **DB**：無 migration
- **第三方**：無

### 優先級評估

- 使用者價值：**中** — 屬「現有功能的破口」：`/auth/callback` 已經會送出這個參數，但沒有任何地方接收，等於驗證失敗的用戶一律靜默卡住
- 開發成本：**S** — 約 10 行內的小修補，單檔 load + 單檔顯示
- 學習收益：SvelteKit `load` 的 error 來源 vs `form` action error 來源的區分、反射型參數的安全處理
- 建議順位：成本最低、且修的是既有 bug，建議第一個做

---

## 跨區塊待釐清

- **信件模板語言**：Supabase 預設驗證信／重設信為英文。是否要在 Supabase 後台中文化模板？— 需要使用者決定（產品決策，非工程阻塞）
- **redirect URL 白名單**：區塊 A 需在 Supabase 後台 Authentication → URL Configuration 加入 `/auth/reset-password`；localhost 與 production 兩組都要。— 需要使用者在 Supabase 後台操作
- **Rate limit 行為**：區塊 A/B 都依賴 Supabase 寄信，Supabase 免費方案寄信有每小時上限。是否需要自架 SMTP（如 Resend）以提高額度與穩定性？— 需要使用者評估，目前用量低可暫不處理
- **重設後 session 失效範圍**：AC 假設 `updateUser({ password })` 會讓其他裝置 session 失效，此為 Supabase 預設行為，實作時需實測驗證
