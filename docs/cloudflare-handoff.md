# Cloudflare 交接 — 把 `tarot.rydercloud.cc` 上線給塔羅 OCI 站

> 這份是給**已登入 Cloudflare MCP 的那個 session**（`oci-arm-host-capacity`）執行用的。
> 另一個 session（tarot-app）負責 OCI 主機設定與 code 改網域，但它**碰不到 Cloudflare MCP**，所以 Cloudflare 這段請你做，做完把 **Origin Certificate 的 PEM** 回貼即可。

## 你需要做的三件事（全在 Cloudflare）

Zone：`rydercloud.cc`
Zone ID：`b9cf9905b5d97550f3b24883a24c5fff`
帳號：rider159159@gmail.com

### 1. 建 DNS 記錄
- Type：`A`
- Name：`tarot`（完整即 `tarot.rydercloud.cc`）
- Content / IP：`134.185.107.141`（OCI ARM 機器）
- **Proxy：開（Proxied / 橘雲）** ← 重要，這是隱藏真實 IP + 走 CF edge 的關鍵
- TTL：Auto

### 2. 產 Origin Certificate
- 用 Cloudflare **Origin CA** 簽一張憑證（不是 Universal SSL，是給回源用的 origin cert）
- Hostnames：`tarot.rydercloud.cc`（建議順手加 `*.rydercloud.cc` 與 `rydercloud.cc`，日後其他子網域可共用）
- Key type：RSA（2048）或 ECDSA 皆可
- 有效期：**15 年**
- 產出兩段 PEM：
  - `certificate`（憑證，公開）
  - `private_key`（私鑰，機密）
- ⚠️ private_key 只會顯示一次，務必完整複製。

> Cloudflare MCP 若沒有直接的 Origin CA 工具，可走 REST：
> `POST https://api.cloudflare.com/client/v4/certificates`
> body：`{"hostnames":["tarot.rydercloud.cc","*.rydercloud.cc"],"requested_validity":5475,"request_type":"origin-rsa","csr":""}`
> （`requested_validity` 單位是天，5475 ≈ 15 年；留空 csr 由 CF 產 key 並一起回傳 private_key。）
> 需要 Origin CA Key 或具 `SSL and Certificates:Edit` 權限的 API token。

### 3. 設 SSL/TLS 模式
- 該 zone 的 SSL/TLS encryption mode 設成 **Full (strict)**
- （可選）開 **Always Use HTTPS**，讓 http 自動轉 https

---

## 做完請回貼這些給 tarot-app session

1. **確認 DNS A record 已建立且 Proxied**（一句話即可）。
2. **Origin Certificate 全文**，格式如下（兩段 PEM 都要）：

```
-----BEGIN CERTIFICATE-----
...(certificate)...
-----END CERTIFICATE-----
```

```
-----BEGIN PRIVATE KEY-----
...(private_key)...
-----END PRIVATE KEY-----
```

3. **確認 SSL 模式已設 Full (strict)**。

> 拿到 cert 後，tarot-app session 會把它寫進 OCI 機器的 `~/projects/tarot-app/caddy/origin.pem` / `origin.key`（權限 600、不進 git），改 Caddyfile 用 `tls` 指這兩個檔，recreate Caddy 容器。

---

# 背景 / 整體進度（讓接手的 session 有完整 context）

## 目標
塔羅 app 已從 Zeabur 遷到 OCI ARM 自架（`134.185.107.141`，Caddy + Docker），現在只能用 `http://134.185.107.141` 純 IP 無加密存取。要把 **`tarot.rydercloud.cc`** 指過去並上 HTTPS。

## 已確認的決策
1. **SSL 架構**：Cloudflare Proxy（橘雲）+ **Origin Certificate**，SSL 模式 **Full (strict)**。
2. **網址**：子網域 `tarot.rydercloud.cc`（根網域留給之後）。
3. **舊網址**：code 裡寫死的 `rtarot.zeabur.app` 改成新網域。

## OCI 機器現況（tarot-app session 已勘查）
- `ssh oci`（= `ssh -i ~/.ssh/oci_instance ubuntu@134.185.107.141`）
- `~/projects/tarot-app` 三個 container 正常跑：frontend(:3000)、backend(:5098)、caddy(對外 :80、:443 已 publish)
- 機器上 `Caddyfile` 目前是 `:80 { ... }`（純 IP 無 TLS）。`Caddyfile` 與 `docker-compose.prod.yml` **只在機器上、repo 沒 commit**。
- ⚠️ **OCI Security List 目前只開 80，443 對外關閉**（實測 nc 443 filtered）。需使用者去 OCI Console 手動開 443 inbound（Source 0.0.0.0/0, TCP, port 443）。本機沒裝 oci CLI，無法程式化開。

## 完整執行順序（跨兩個 session + 使用者）
1. **(你，這份文件)** Cloudflare：建 DNS A record（Proxied）+ 產 Origin Cert + 設 Full(strict) → 回貼 cert。
2. **(使用者)** OCI Console 開 443 inbound。
3. **(tarot-app session)** 把 cert 傳上機器、改機器 Caddyfile / compose / .env、recreate caddy + 重啟 frontend。
4. **(tarot-app session)** 本機改 `frontend/src/lib/seo/config.ts`（`url`）和 `backend/TarotApi/Services/PromptBuilder.cs`（`Source`）的網域 → commit + push → 機器 pull → rebuild。
5. **(tarot-app session)** 端到端驗證（dig / nc 443 / curl https / api/health / SEO）。

## 機器端待套用的設定（tarot-app session 已準備好範本，等 cert）
- Caddyfile 改成：
  ```
  tarot.rydercloud.cc {
      tls /etc/caddy/origin.pem /etc/caddy/origin.key
      handle /api/* { reverse_proxy backend:5098 }
      handle { reverse_proxy frontend:3000 }
  }
  ```
- compose 的 caddy service 加 volume 掛 cert；frontend 的 `ORIGIN` 改 `https://tarot.rydercloud.cc`
- `.env` 的 `ALLOWED_ORIGINS` 改 `https://tarot.rydercloud.cc`
