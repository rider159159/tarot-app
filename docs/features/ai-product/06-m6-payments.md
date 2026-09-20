# M6｜儲值金流（IPaymentProvider ＋ Stripe Checkout 測試模式）

> 目標：`IPaymentProvider` 抽象 ＋ Stripe Hosted Checkout 買點數包 ＋ webhook 冪等加點。
> ⚠️ **重要背景（已查證）**：Stripe 不支援台灣主體帳號，本 milestone 只做**測試模式**（sk_test）。正式收款要另行拍板：Polar（MoR，4%+$0.40，官方支援台灣+TWD 出金）／Lemon Squeezy（MoR，5%+$0.50+1%）／綠界個人。介面已隔離，屆時換一個 provider class 即可。
> 前置：M2 完成（`payment_events` 表已在 008 建好、`GrantAsync` 已有）。

## 0. 前置條件
- 使用者提供 Stripe 測試金鑰：`STRIPE_SECRET_KEY=sk_test_...`。沒有就停下來要（註冊 stripe.com 即可拿測試金鑰，不需台灣收款資格）。
- 本地 webhook 測試需要 Stripe CLI（`brew install stripe/stripe-cli/stripe`）。

## 1. Migration
無（`payment_events` 已存在）。

## 2. 後端

### 2.1 安裝 SDK
```bash
cd backend/TarotApi && dotnet add package Stripe.net
```

### 2.2 點數包目錄（後端決定價格，絕不信前端）

新檔 `Services/Payments/CreditPacks.cs`：

```csharp
namespace TarotApi.Services.Payments;

public record CreditPack(string Id, int Credits, int PriceTwd);

public static class CreditPacks
{
    // env 格式：packId:credits:TWD;packId:credits:TWD
    public static readonly IReadOnlyDictionary<string, CreditPack> All = Parse(
        Environment.GetEnvironmentVariable("CREDIT_PACKS") ?? "starter:60:99;value:200:299");

    private static Dictionary<string, CreditPack> Parse(string raw) =>
        raw.Split(';', StringSplitOptions.RemoveEmptyEntries)
           .Select(p => p.Split(':'))
           .ToDictionary(a => a[0], a => new CreditPack(a[0], int.Parse(a[1]), int.Parse(a[2])));
}
```

### 2.3 新檔 `Services/Payments/IPaymentProvider.cs`

```csharp
namespace TarotApi.Services.Payments;

public interface IPaymentProvider
{
    /// <summary>建立結帳流程，回傳導向 URL。</summary>
    Task<string> CreateCheckoutAsync(Guid userId, CreditPack pack, CancellationToken ct);
    /// <summary>處理 webhook：驗簽 → 去重 → 加點。回傳 HTTP status。</summary>
    Task<int> HandleWebhookAsync(HttpRequest request, CancellationToken ct);
}
```

### 2.4 新檔 `Services/Payments/StripePaymentProvider.cs`

```csharp
using Stripe;
using Stripe.Checkout;
using TarotApi.Data;

namespace TarotApi.Services.Payments;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly TarotDbContext _db;
    private readonly CreditService _credits;
    private readonly ILogger<StripePaymentProvider> _logger;
    private readonly string _webhookSecret;
    private readonly string _origin;

    public StripePaymentProvider(TarotDbContext db, CreditService credits,
        ILogger<StripePaymentProvider> logger)
    {
        _db = db; _credits = credits; _logger = logger;
        StripeConfiguration.ApiKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY is required");
        _webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET") ?? "";
        _origin = Environment.GetEnvironmentVariable("ORIGIN") ?? "http://localhost:5173";
    }

    public async Task<string> CreateCheckoutAsync(Guid userId, CreditPack pack, CancellationToken ct)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            ClientReferenceId = userId.ToString(),
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "twd",
                        UnitAmount = pack.PriceTwd * 100,   // TWD 以「分」計，需為 100 的倍數 → 定價取整數 TWD
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"點數包 {pack.Id}（{pack.Credits} 點）"
                        }
                    }
                }
            ],
            Metadata = new() { ["packId"] = pack.Id, ["credits"] = pack.Credits.ToString() },
            SuccessUrl = $"{_origin}/profile?topup=success",
            CancelUrl = $"{_origin}/profile?topup=cancel"
        };
        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
        return session.Url;
    }

    public async Task<int> HandleWebhookAsync(HttpRequest request, CancellationToken ct)
    {
        string json;
        using (var reader = new StreamReader(request.Body))
            json = await reader.ReadToEndAsync(ct);

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json, request.Headers["Stripe-Signature"], _webhookSecret);
        }
        catch (StripeException e)
        {
            _logger.LogWarning(e, "Stripe webhook signature verification failed");
            return StatusCodes.Status400BadRequest;
        }

        if (stripeEvent.Type != "checkout.session.completed")
            return StatusCodes.Status200OK;   // 不關心的事件直接 ack

        // 去重：payment_events (provider, event_id) 主鍵擋重送
        var inserted = await _db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO payment_events (provider, event_id)
            VALUES ('stripe', {stripeEvent.Id}) ON CONFLICT DO NOTHING");
        if (inserted == 0) return StatusCodes.Status200OK;   // 已處理過

        var session = (Session)stripeEvent.Data.Object;
        if (!Guid.TryParse(session.ClientReferenceId, out var userId)
            || !session.Metadata.TryGetValue("packId", out var packId)
            || !CreditPacks.All.TryGetValue(packId, out var pack))
        {
            _logger.LogError("Stripe webhook payload invalid: session {SessionId}", session.Id);
            return StatusCodes.Status200OK;   // 資料異常記 log，仍 ack 避免無限重送
        }

        await _credits.GrantAsync(userId, pack.Credits, "topup",
            $"stripe:{stripeEvent.Id}", refType: "payment", refId: session.Id);
        _logger.LogInformation("Topup {Credits} credits for {UserId} via {SessionId}",
            pack.Credits, userId, session.Id);
        return StatusCodes.Status200OK;
    }
}
```

註冊（`Program.cs`）：

```csharp
builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
```

### 2.5 Endpoints

`CreditsController` 加：

```csharp
public record CheckoutRequestDto(string PackId);

[HttpGet("packs")]
public IActionResult GetPacks() =>
    Ok(CreditPacks.All.Values.Select(p => new { p.Id, p.Credits, p.PriceTwd }));

[HttpPost("checkout")]
public async Task<IActionResult> Checkout([FromBody] CheckoutRequestDto dto, CancellationToken ct)
{
    if (!CreditPacks.All.TryGetValue(dto.PackId, out var pack))
        return BadRequest(new { code = "UNKNOWN_PACK" });
    var url = await _payment.CreateCheckoutAsync(User.GetUserId(), pack, ct);
    return Ok(new { url });
}
```

新檔 `Controllers/PaymentsController.cs`：

```csharp
[ApiController]
[Route("api/payments")]
public class PaymentsController(IPaymentProvider payment) : ControllerBase
{
    [HttpPost("webhook/stripe")]
    [AllowAnonymous]                       // 靠簽章驗證，不靠 JWT
    public async Task<IActionResult> StripeWebhook(CancellationToken ct)
        => StatusCode(await payment.HandleWebhookAsync(Request, ct));
}
```

## 3. 前端

- Proxy：`src/routes/api/credits/packs/+server.ts`（GET）、`src/routes/api/credits/checkout/+server.ts`（POST）——普通 JSON 轉發。
- `/profile` 加「儲值」區塊：列出點數包（GET packs）→ 點選 → POST checkout → `window.location.href = url`。
- 回站處理：`/profile` 讀 query `topup=success` 顯示「儲值完成，點數已入帳」（webhook 是非同步的，加一句「若餘額未更新請稍後重整」）；`topup=cancel` 顯示已取消。

## 4. 驗證（Stripe 測試模式）

```bash
stripe login
stripe listen --forward-to localhost:5098/api/payments/webhook/stripe
# 把它印出的 whsec_... 放進 .env 的 STRIPE_WEBHOOK_SECRET，重啟 backend
```

1. 瀏覽器：/profile → 選點數包 → 導去 Stripe Checkout → 測試卡 `4242 4242 4242 4242` 付款 → 回站 → 餘額增加。
2. 冪等：`stripe events resend <event_id>`（或 CLI 重播）→ 餘額**不再**增加；`payment_events` 只有一筆。
3. 竄改測試：直接 curl 打 webhook（無簽章）→ 400。
4. `SELECT * FROM credit_transactions WHERE reason='topup';` 有正確一筆。

## 5. 禁區
- 不做訂閱、不做退款流程、不做發票。
- 不切 live 金鑰（台灣主體無法啟用；正式收款等拍板）。
- 價格/點數對應只存在後端 env——前端傳來的任何金額一律不信。

## 6. 常見坑
- **webhook 必須讀 raw body 驗簽**——不要讓 model binding 先把 body 吃掉（本文件的寫法直接讀 `Request.Body`，安全；不要在 action 參數上加 `[FromBody]`）。
- 反代路徑：prod 的 Stripe webhook URL 是 `https://tarot.rydercloud.cc/api/payments/webhook/stripe`（走既有 `/api/*` 反代），Cloudflare proxy 不影響 Stripe 送達。
- TWD 在 Stripe 是特殊幣別：`UnitAmount` 要是 100 的倍數（上面已 ×100，定價保持整數 TWD 即可）。
- `GrantAsync` 冪等 key 用 `stripe:{eventId}`，與 `payment_events` 雙保險——兩層都要在。

## 7. Definition of Done
- [ ] 測試模式完整走通：買包→webhook→加點；重送不重複加；無簽章被拒
- [ ] `.env.example` 補 M6 變數；`CLAUDE.md` API 表加 packs/checkout/webhook 三條
- [ ] 文件記錄「正式收款待拍板」選項（本檔開頭）保持最新
- [ ] 跑過 `/qa`
