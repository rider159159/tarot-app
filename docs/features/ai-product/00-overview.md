# AI 占卜產品化（L3–L5）— 總覽與全域慣例

> 本資料夾是「抽牌工具 → AI 占卜產品」的施工文件組，供執行模型（如 Haiku/Sonnet 級）逐份執行。
> 每份 milestone 文件自成一體：照順序做、不要自行擴充範圍。
> 產出依據：2026-07-15 對 repo、線上 Supabase DB、外部服務限制的實際查證（非憑空規劃）。

## 0. 現況基準（已驗證，執行模型不必重查）

- Migration 位於 `supabase/migrations/`，目前到 `006_soft_delete_readings.sql`；**線上 DB 已套用到 006**（readings 已有 `interpretation`、`notes`、`client_token`、`deleted_at`）。下一號從 **007** 開始。
- 後端已有：JWKS JWT 驗證（全域 `[Authorize]`）、rate limiter policy `"anonymous-draw"`（10 req/min/IP）、`ExceptionHandlingMiddleware`、`PromptBuilder`（只做匯出，`BuildSingleExport` / `BuildReadingDto`）、`POST /api/tarot/draw`（匿名）、`POST /api/readings/import`（client_token 冪等）。
- 後端**沒有**任何 AI / LLM / SSE 程式碼；前端**沒有**任何 EventSource / ReadableStream 程式碼。全部是新做。
- 前端：Svelte 5 runes（`$props`/`$state`/`$derived`/`$effect`）、`locals.safeGetSession()` 取 session、`createServerApiClient(accessToken)`（get/post/put/delete）、`hooks.server.ts` 的 `PUBLIC_PATHS` 控路由。
- `SpreadType` enum（後端）：`Single, ThreeCardTime, ThreeCardProblem, ThreeCardLinear, CelticCross, WeeklyFortune, Custom`；前端 key：`single | three-card-time | three-card-problem | three-card-linear | celtic-cross | weekly-fortune | custom`。
- 部署：OCI ARM + Docker（`docker-compose.prod.yml`，backend 用 `env_file: .env`）＋ 反代 Nginx 在**另一個 repo `oci-infra`** ＋ Cloudflare proxy。

## 1. 里程碑地圖與依賴

| # | 文件 | 內容 | Migration | 依賴 |
|---|------|------|-----------|------|
| M1 | `01-m1-ai-interpret.md` | IAiClient（Claude）＋ AI 解讀 SSE 串流（先免費） | 007 | 無 |
| M2 | `02-m2-credits.md` | 點數錢包/帳本/每日領取，把 M1 接上扣點 | 008 | M1 |
| M3 | `03-m3-chat.md` | 解讀後多輪追問（SSE＋扣點） | 009 | M1、M2 |
| M4 | `04-m4-intake.md` | 抽牌前澄清對話（免費、匿名可用） | 無（007 已含 context 欄位） | M1 |
| M5 | `05-m5-memory.md` | 跨次記憶 Soul Profile | 010 | M1（建議在 M3 後） |
| M6 | `06-m6-payments.md` | IPaymentProvider ＋ Stripe Checkout（測試模式） | 無（008 已含 payment_events） | M2 |
| M7 | `07-m7-draw-experience.md` | 抽牌儀式感（純前端） | 無 | 無（隨時可插隊） |

執行方式建議：**一個 milestone 開一個乾淨 session**，指令：

```
/build 依 docs/features/ai-product/01-m1-ai-interpret.md 實作，遵守文件內的禁區與 DoD
```

完成後跑 `/qa`（八大清單），通過再進下一個 milestone。

## 2. 已拍板決策與預設值（可改，改了請同步更新本表）

| 項目 | 預設值 | 說明 |
|------|--------|------|
| AI 供應商 | Anthropic（抽 `IAiClient` 介面） | **實作用官方 .NET SDK（NuGet `Anthropic`）**，不手刻 HttpClient/SSE 解析——錯誤面小非常多。OpenAI 實作先不寫，介面留著即可 |
| `AI_MODEL_INTERPRET` | `claude-sonnet-4-6`（$3/$15 per MTok） | 解讀/追問用。若盲測品質不夠，升 `claude-opus-4-8`（$5/$25），改 env 即可 |
| `AI_MODEL_LIGHT` | `claude-haiku-4-5`（$1/$5 per MTok） | intake、記憶摘要用 |
| 抽牌 | 永遠免費（匿名＋登入） | 不動現有流程 |
| AI 解讀/追問 | 需登入＋扣點 | 匿名者看到「登入後可用 AI 解讀」導去登入（沿用 anonymous→import 漏斗） |
| `CREDIT_SIGNUP_BONUS` | 5 | 新戶起始贈點 |
| `CREDIT_DAILY_CLAIM` | 1 | 每日領取（台北時區一天一次） |
| `CREDIT_INTERPRET_COST` | 1 | 每個 reading 一次，冪等不重複扣 |
| `CREDIT_CHAT_COST` | 1 | 每則使用者追問 |
| 點數 UI 名稱 | 「點數」（佔位） | ⚠️ 待使用者拍板正式名稱（靈感/星塵/…），程式內請用 `credits` 一詞，UI 文案集中好改 |
| intake | 免費、可選入口（非強制步驟）、匿名可用 | 每 IP 10 req/min（新 rate limit policy `"intake"`） |
| 記憶 | 預設開啟，個人頁可查看/清除 | 隱私賣點：透明可控 |
| 金流 | Stripe Checkout **測試模式** | ⚠️ Stripe 不支援台灣帳號，正式收款須另行拍板（見 §4） |
| 點數包定價 | `starter:60:99`、`value:200:299`（佔位） | ⚠️ 待拍板。格式 `packId:credits:TWD` |
| 解讀 persona | 先一個寫死的預設 persona | 多 persona 選擇留到之後 |

## 3. 資源限制與成本（查證結果）

### Supabase（免費方案，專案 `tfmvmunpqsbaynreuxme`，目前 ACTIVE_HEALTHY）
- **500 MB DB**：純文字資料撐很久（解讀 ~2KB/筆、訊息 ~0.5KB/筆 → 十萬筆等級才有感）。短期無虞。
- **7 天低活動會被暫停**：暫停 = auth + DB 全掛 = 整站死，需手動去 dashboard restore。早期低流量是真實風險。對策擇一：(a) GitHub Actions 每日 cron 打一次 Supabase REST（帶 anon key）當 keep-alive；(b) 有真實使用者後升 Pro（US$25/月）一勞永逸。
- **內建信箱每小時只能寄 2 封 auth 信**（2024-09 起）：Email 註冊量一大就爆。**正式對外前必須接自訂 SMTP**（Resend/SES 等，接上後預設 30 新用戶/小時、可再調）。Google OAuth 註冊不吃這個額度，已有 OAuth 是好事。
- 50k MAU、5GB egress：綽綽有餘。
- 結論：**免費方案可以開發到 M6 全部完成**；「有真實使用者、要對外宣傳」是升 Pro 的時間點。

### Anthropic API（唯一「每用必花」的新成本，無免費額度）
| 動作 | 模型 | 估算 tokens（in/out） | 每次成本 |
|------|------|----------------------|----------|
| AI 解讀 | Sonnet 4.6 | ~2.5k / ~1.2k | ~US$0.026（≈NT$0.8） |
| 追問一則 | Sonnet 4.6 | ~3k / ~0.6k | ~US$0.018（≈NT$0.6） |
| intake 一輪 | Haiku 4.5 | ~1k / ~0.25k | ~US$0.002（≈NT$0.07） |
| 記憶更新 | Haiku 4.5 | ~1.5k / ~0.4k | ~US$0.004（≈NT$0.11） |

月估：30 個活躍使用者（各 10 次解讀＋20 追問＋30 intake）≈ **US$20–25/月**。點數制天然對沖這個成本——1 點成本 <NT$1，定價空間大。API key 需在 console.anthropic.com 儲值（pay-as-you-go）。

### 金流（⚠️ 影響 M6 決策）
- **Stripe 不支援台灣主體帳號**（2026 年仍是，46 國名單無台灣）。要用 Stripe 正式收款 = 開美國 LLC，對 side project 不划算。
- 可行替代（台灣個人可用）：
  - **Polar**：Merchant of Record，4% + $0.40，官方文件明列台灣＋TWD 出金，開發者導向（hosted checkout + webhook，DX 接近 Stripe）。
  - **Lemon Squeezy**：MoR，5% + $0.50 +國際出金 1%，PayPal 出金台灣可用。
  - **綠界 ECPay 個人賣家**：TWD 在地、免公司，但手續費較高且有額度限制，介接文件較舊式。
- M6 的做法：**照原規劃用 Stripe 測試模式把 `IPaymentProvider` 骨架做完**（學習成本最低、文件最好），正式收款前再拍板換 Polar/綠界——介面已隔離，換供應商只動一個 class。

### OCI / 反代 / Cloudflare（SSE 串流路徑）
- OCI ARM 免費額度（4 OCPU/24GB）遠超需求，無虞。
- 後端 SSE 回應會帶 `X-Accel-Buffering: no` header，**Nginx 預設會尊重它自動關 buffering，理論上 `oci-infra` 不用改**。驗收若出現「串流卡住、最後一次全吐」，才去 `oci-infra` 檢查該 server block 是否有 `proxy_ignore_headers X-Accel-Buffering` 或把 `text/event-stream` 加進 gzip；改了 infra 必須 commit + push `oci-infra` repo（全域規則）。
- Cloudflare proxy 對 `text/event-stream` 不做 buffering；idle 100 秒才斷線，token 流不會 idle 那麼久，安全。

## 4. 全域施工慣例（每份 milestone 文件都遵守）

### 4.1 動工順序（不可顛倒）
`migration 寫好 → 實際套用到線上 DB → 後端 → 前端`。migration 沒套用就部署依賴新欄位的後端 = runtime 約束違規（custom-spread 500 事件前車之鑑）。

### 4.2 Migration 套用與驗證
1. 檔案放 `supabase/migrations/00X_名稱.sql`（下一號 007）。
2. 套用：用 Supabase MCP 的 `apply_migration`（name 用檔名去掉 `.sql`），或貼到 Dashboard SQL Editor 執行。
3. 驗證：`list_migrations` 看到新條目、`list_tables` 看到新欄位/表才算完成。
4. 新表一律 `ENABLE ROW LEVEL SECURITY` 且**不加 policy**（擋 anon key 走 PostgREST 直捅；後端走 `SUPABASE_DB_CONNECTION_STRING` 直連不受影響——與 002 慣例一致）。

### 4.3 SSE 統一格式（interpret 與 chat 共用）
- Response headers：`Content-Type: text/event-stream; charset=utf-8`、`Cache-Control: no-cache`、`X-Accel-Buffering: no`。
- 每個 event 一行 `data: {JSON}\n\n`，JSON 形狀只有三種：
  - `{"delta":"文字片段"}`
  - `{"done":true}`（正常結束，永遠是最後一個）
  - `{"error":"訊息","code":"ERROR_CODE"}`（發生錯誤，之後即結束）
- 前端一律用 `lib/utils/sse.ts` 的 `readSse()`（M1 建立）解析，不要每處自己寫 parser。
- SvelteKit proxy `+server.ts` 直接 `return new Response(upstream.body, {...})` pipe 給瀏覽器，不落地緩衝。

### 4.4 冪等 key 命名（credit_transactions.idempotency_key）
| 動作 | key 格式 |
|------|----------|
| 起始贈點 | `signup_bonus:{userId}` |
| 每日領取 | `daily:{userId}:{yyyy-MM-dd}`（台北時區日期） |
| 解讀扣點 | `interpret:{readingId}` |
| 解讀退點 | `refund:interpret:{readingId}` |
| 追問扣點 | `chat:{readingId}:{clientMessageId}` |
| 追問退點 | `refund:chat:{readingId}:{clientMessageId}` |
| 儲值加點 | `stripe:{eventId}` |

### 4.5 錯誤碼（HTTP + body `{ "code": "...", "message": "..." }`）
| Code | HTTP | 場景 |
|------|------|------|
| `INSUFFICIENT_CREDITS` | 402 | 餘額不足 |
| `ALREADY_CLAIMED` | 409 | 今日已領 |
| `INTERPRET_IN_PROGRESS` | 409 | 同一 reading 併發解讀 |
| `AI_UNAVAILABLE` | 503 | AI 呼叫失敗（已退點） |

### 4.6 新增環境變數總表（每個 milestone 動到就同步 `.env.example`；密鑰只進本地 `.env`，絕不進版控）
```
# M1
ANTHROPIC_API_KEY=
AI_PROVIDER=claude
AI_MODEL_INTERPRET=claude-sonnet-4-6
AI_MODEL_LIGHT=claude-haiku-4-5
# M2
CREDIT_SIGNUP_BONUS=5
CREDIT_DAILY_CLAIM=1
CREDIT_INTERPRET_COST=1
CREDIT_CHAT_COST=1
# M6
PAYMENT_PROVIDER=stripe
STRIPE_SECRET_KEY=
STRIPE_WEBHOOK_SECRET=
CREDIT_PACKS=starter:60:99;value:200:299
```
prod 部署：backend 走 `env_file: .env`，把新變數加進 OCI 機器上的 `.env` 即可，`docker-compose.prod.yml` 與前端 build args 都不用動。

### 4.7 其他紀律
- 遵守 `.claude/commands/build.md` 五條紀律（一改動一 commit、先驗證再 commit、前後端型別同輪改、新套件先查官方文檔、非顯而易見的決策主動問）。
- AI 呼叫一律**不送 `temperature`**（保持跨模型相容；Opus 4.7+ 送了會 400）。
- `MaxTokens` 上限寫在呼叫端常數（解讀 2000 / 追問 1000 / intake 600 / 記憶 800），控成本。
- 所有新 API 進 `CLAUDE.md` 的 endpoint 表（每個 milestone 的 DoD 有列）。

## 5. 驗證關卡（M1 完成後就做，不要等全部做完）
找 5–10 人，同一個問題各抽一次：本站 AI 解讀 vs MysticX 解讀並排盲測。「解牌品質站得住」是整個付費策略的地基；不站得住就先調 prompt/換 `claude-opus-4-8`，再繼續 M2+。

## 6. 仍待使用者拍板（不擋 M1–M5 開工）
1. 點數 UI 正式名稱（目前佔位「點數」）。
2. 點數包數量與定價（目前佔位兩包）。
3. 正式收款管道：Polar / Lemon Squeezy / 綠界個人 / 開美國公司用 Stripe（M6 測試模式不受影響）。
4. 點數初值若要調整（§2 表格）。
