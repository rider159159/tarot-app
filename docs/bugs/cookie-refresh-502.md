# Bug：登入 cookie 過期時整站噴 502（Cloudflare / Nginx Bad Gateway）

## 症狀
使用者登入著的時候一切正常；一旦 session cookie 接近／超過到期，下一次開頁面就整站噴 **502**（Cloudflare 的 Bad Gateway 頁，非 SvelteKit 的錯誤頁）。重新登入後又恢復，過一小時再犯。

## 根因
鏈路是 **Cloudflare → Nginx（oci-infra）→ frontend(:3000)**。

1. `@supabase/ssr` 預設把**整個 session 序列化進 cookie**，包含一份約 1KB 的 `user` 物件（`user-and-tokens` 編碼），單顆 auth cookie 因此約 2.8KB。
2. access_token 每小時到期。到期前 90 秒（`EXPIRY_MARGIN_MS`）內，`hooks.server.ts` 的 `safeGetSession()` 會在**伺服器端**觸發 token refresh，把刷新後的大 cookie 用 `Set-Cookie` 寫回回應。
3. Nginx 反向代理必須把**整包 response header**（含那條大 `Set-Cookie`）讀進**單一一塊 `proxy_buffer_size` 緩衝區**，預設僅 **4KB**。實測過期 refresh 的 header 約 4.6KB（真實 2-chunk session 可達 5–8KB）→ 超過 4KB → Nginx 以 `upstream sent too big header while reading response header from upstream` 中止 → 502。

**只在 cookie 過期時發作**，因為那是唯一會「寫新 cookie 進回應」的時機；cookie 有效時不 refresh、回應 header 僅約 1.7KB，所以正常。問題在遷到自架 Nginx（預設 buffer 較小）後才浮現。

確認方式（機器上）：
```bash
docker logs <nginx 容器> 2>&1 | grep "too big header"
```

## 修法 A：Nginx 加大 buffer（根治，需在 oci-infra repo 套用）
反代到 frontend 的區塊加上：
```nginx
proxy_buffer_size        16k;   # 真正修好 502 的就這條：header 必須塞進這一塊
proxy_buffers            8 16k;
proxy_busy_buffers_size  16k;
large_client_header_buffers 4 16k;   # http/server 層級；放大進來的大 Cookie 請求，避免 400
```
`nginx -t && nginx -s reload` 生效。這是穩健解：不改 app、覆蓋 2-chunk 與未來成長。

## 修法 B：app 改 tokens-only（瘦身，已在本 repo 套用）
讓 `user` 物件不進 cookie（改存記憶體／localStorage），cookie 從 2.8KB 降到約 1.5KB、refresh header 從 4.6KB 降到約 3.3KB（< 4KB）。

- `frontend/src/hooks.server.ts`：server client 的 `cookies` 加 `encode: 'tokens-only'`。
- `frontend/src/lib/supabase.ts`：browser client 一致設 `encode: 'tokens-only'`，並補上 getAll/setAll 與 SSR-safe `userStorage`（否則 `createBrowserClient` 在 server import 時存取 `window.localStorage` 會 throw）。
- 新增直接依賴 `cookie`（browser 端 cookie 序列化用）。

注意事項：
- 兩個 client 的 `encode` 必須一致，否則 cookie 格式不一致會造成 stale chunk → 隨機登出。
- `encode` 是 `@supabase/ssr` 的 `@experimental` 選項。
- access_token（JWT）仍留在 cookie，是大小的地板；若 JWT 很大或 session 切成 2 chunk，B 仍可能破 4KB——所以 A 才是根治，B 為輔。
- 全專案不讀 `session.user`（user 一律來自 `getUser()` → `data.user`），故此改動不影響功能。
