# M2｜點數骨架（錢包／帳本／每日領取，並接上解讀扣點）

> 目標：帳本式點數系統（唯一真相是 `credit_transactions`，餘額是快取），新戶贈點、每日領取，M1 的解讀開始扣點。
> 前置：M1 完成。先讀 `00-overview.md` §4（特別是冪等 key 表與錯誤碼表）。

## 1. Migration `008_credits.sql`

```sql
-- 008_credits.sql
-- 點數：錢包(快取) + 帳本(真相) + 每日領取 + 金流事件去重(M6 用)

CREATE TABLE credit_accounts (
  user_id    uuid PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  balance    int  NOT NULL DEFAULT 0 CHECK (balance >= 0),
  updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE credit_transactions (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id         uuid NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  amount          int  NOT NULL,            -- 正=加點 負=扣點
  reason          text NOT NULL,            -- signup_bonus|daily_claim|topup|interpret|chat|refund
  ref_type        text,
  ref_id          text,
  idempotency_key text UNIQUE,
  created_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX idx_credit_tx_user ON credit_transactions (user_id, created_at DESC);

CREATE TABLE daily_claims (
  user_id    uuid NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
  claim_date date NOT NULL,                 -- 台北時區日期
  amount     int  NOT NULL,
  PRIMARY KEY (user_id, claim_date)
);

CREATE TABLE payment_events (
  provider     text NOT NULL,
  event_id     text NOT NULL,
  processed_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (provider, event_id)
);

ALTER TABLE credit_accounts     ENABLE ROW LEVEL SECURITY;
ALTER TABLE credit_transactions ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_claims        ENABLE ROW LEVEL SECURITY;
ALTER TABLE payment_events      ENABLE ROW LEVEL SECURITY;
```

套用並驗證後才動後端。

## 2. 後端

### 2.1 Entity ＋ DbContext

`Models/CreditAccount.cs`：`UserId (Guid, PK)`、`Balance (int)`、`UpdatedAt`。
`Models/CreditTransaction.cs`：對映 `credit_transactions` 全欄位。
`TarotDbContext` 加 `DbSet<CreditAccount> CreditAccounts`、`DbSet<CreditTransaction> CreditTransactions`，snake_case 對映照 `Reading` 既有寫法。（`daily_claims`、`payment_events` 只用 raw SQL，不建 entity。）

### 2.2 新檔 `Services/CreditService.cs`（核心，整段照抄後依編譯器微調）

```csharp
using Microsoft.EntityFrameworkCore;
using TarotApi.Data;

namespace TarotApi.Services;

public enum ChargeResult { Success, InsufficientCredits, AlreadyProcessed }

public class CreditService(TarotDbContext db)
{
    public static readonly int SignupBonus   = EnvInt("CREDIT_SIGNUP_BONUS", 5);
    public static readonly int DailyClaim    = EnvInt("CREDIT_DAILY_CLAIM", 1);
    public static readonly int InterpretCost = EnvInt("CREDIT_INTERPRET_COST", 1);
    public static readonly int ChatCost      = EnvInt("CREDIT_CHAT_COST", 1);
    private static int EnvInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var v) ? v : fallback;

    private static readonly TimeZoneInfo Taipei = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");
    public static DateOnly TodayTaipei() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Taipei));

    /// <summary>確保錢包存在＋發過起始贈點（兩者皆冪等）。所有入口先呼叫這個。</summary>
    public async Task EnsureAccountAsync(Guid userId)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO credit_accounts (user_id, balance) VALUES ({userId}, 0) ON CONFLICT (user_id) DO NOTHING");
        await GrantAsync(userId, SignupBonus, "signup_bonus", $"signup_bonus:{userId}");
    }

    /// <summary>扣點。同一 transaction 內：帳本插入(冪等) → 條件更新餘額。</summary>
    public async Task<ChargeResult> ChargeAsync(
        Guid userId, int cost, string reason, string idemKey, string? refType = null, string? refId = null)
    {
        await EnsureAccountAsync(userId);
        await using var tx = await db.Database.BeginTransactionAsync();

        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO credit_transactions (user_id, amount, reason, ref_type, ref_id, idempotency_key)
            VALUES ({userId}, {-cost}, {reason}, {refType}, {refId}, {idemKey})
            ON CONFLICT (idempotency_key) DO NOTHING");
        if (inserted == 0) { await tx.RollbackAsync(); return ChargeResult.AlreadyProcessed; }

        var updated = await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE credit_accounts SET balance = balance - {cost}, updated_at = now()
            WHERE user_id = {userId} AND balance >= {cost}");
        if (updated == 0) { await tx.RollbackAsync(); return ChargeResult.InsufficientCredits; }

        await tx.CommitAsync();
        return ChargeResult.Success;
    }

    /// <summary>加點（贈點/每日/退點/儲值），冪等。回傳 true=真的加了, false=key 已處理過。</summary>
    public async Task<bool> GrantAsync(
        Guid userId, int amount, string reason, string idemKey, string? refType = null, string? refId = null)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO credit_transactions (user_id, amount, reason, ref_type, ref_id, idempotency_key)
            VALUES ({userId}, {amount}, {reason}, {refType}, {refId}, {idemKey})
            ON CONFLICT (idempotency_key) DO NOTHING");
        if (inserted == 0) { await tx.RollbackAsync(); return false; }

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO credit_accounts (user_id, balance) VALUES ({userId}, {amount})
            ON CONFLICT (user_id) DO UPDATE
              SET balance = credit_accounts.balance + {amount}, updated_at = now()");
        await tx.CommitAsync();
        return true;
    }

    public async Task<int> GetBalanceAsync(Guid userId)
    {
        await EnsureAccountAsync(userId);
        return await db.CreditAccounts.Where(a => a.UserId == userId)
            .Select(a => a.Balance).FirstAsync();
    }

    /// <summary>每日領取。回傳領到的點數；今天領過回 null。</summary>
    public async Task<int?> ClaimDailyAsync(Guid userId)
    {
        await EnsureAccountAsync(userId);
        var today = TodayTaipei();
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO daily_claims (user_id, claim_date, amount)
            VALUES ({userId}, {today}, {DailyClaim}) ON CONFLICT DO NOTHING");
        if (inserted == 0) return null;
        await GrantAsync(userId, DailyClaim, "daily_claim", $"daily:{userId}:{today:yyyy-MM-dd}");
        return DailyClaim;
    }
}
```

`Program.cs`：`builder.Services.AddScoped<CreditService>();`

### 2.3 新檔 `Controllers/CreditsController.cs`

```csharp
[ApiController]
[Route("api/credits")]
public class CreditsController(CreditService credits) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
        => Ok(new { balance = await credits.GetBalanceAsync(User.GetUserId()) });

    [HttpPost("claim-daily")]
    public async Task<IActionResult> ClaimDaily()
    {
        var granted = await credits.ClaimDailyAsync(User.GetUserId());
        if (granted is null)
            return Conflict(new { code = "ALREADY_CLAIMED", message = "今天已經領過囉" });
        return Ok(new { granted, balance = await credits.GetBalanceAsync(User.GetUserId()) });
    }
}
```

### 2.4 把 M1 的 interpret 接上扣點

`ReadingController.Interpret` 的 `[M2 插入點 A]`（在 `StartSse()` 之前）：

```csharp
var charge = await _credits.ChargeAsync(userId, CreditService.InterpretCost,
    "interpret", $"interpret:{id}", refType: "reading", refId: id.ToString());
if (charge == ChargeResult.InsufficientCredits)
    return StatusCode(402, new { code = "INSUFFICIENT_CREDITS", message = "點數不足" });
if (charge == ChargeResult.AlreadyProcessed)
    return Conflict(new { code = "INTERPRET_IN_PROGRESS", message = "解讀進行中，請稍候" });
    // 註：解讀完成的重複請求不會走到這裡（前面已用存檔的 interpretation 短路回傳）
```

`[M2 插入點 B]`（AI 失敗 catch 內）：

```csharp
await _credits.GrantAsync(userId, CreditService.InterpretCost, "refund",
    $"refund:interpret:{id}", refType: "reading", refId: id.ToString());
```

注入 `CreditService _credits`。

## 3. 前端

### 3.1 餘額進 layout
`src/routes/+layout.server.ts`（看現有實作，載入 session 的地方）：有 session 時經 `createServerApiClient(session.access_token).get('/api/credits/balance')` 取餘額，放進回傳資料；失敗就給 `null`（不要讓餘額掛掉整頁）。

### 3.2 新元件 `src/lib/components/CreditBalance.svelte`
- 顯示 `✦ {balance}`（UI 文案先用「點數」）。
- 掛進 `Navbar.svelte` 登入狀態那側。

### 3.3 每日領取
放在 `/profile`：「每日領取」按鈕 → 新 proxy `src/routes/api/credits/claim-daily/+server.ts`（POST，模式照 M1 interpret proxy，但這是普通 JSON 不是 SSE：直接把 upstream 的 status/body 轉回去）→ 成功顯示「+1」與新餘額；409 顯示「今天已領過」。

### 3.4 點數不足 UX
M1 的 `interpret()` 已處理 402 → 把訊息改成含行動：「點數不足——到個人頁領今日點數」（連結 `/profile`）。

## 4. 驗證

```bash
cd backend/TarotApi && dotnet build && cd ../../frontend && pnpm check
```

拿 `TOKEN`（CLAUDE.md 測試帳號）後依序驗：

```bash
B=http://localhost:5098
curl -s -H "Authorization: Bearer $TOKEN" $B/api/credits/balance          # 首次 → {"balance":5}
curl -s -X POST -H "Authorization: Bearer $TOKEN" $B/api/credits/claim-daily   # → granted:1, balance:6
curl -s -X POST -H "Authorization: Bearer $TOKEN" $B/api/credits/claim-daily   # → 409 ALREADY_CLAIMED
# 新 reading 解讀一次 → balance 減 1；同一 reading 再解讀 → balance 不變
# 併發防重扣：
curl -s -X POST -H "Authorization: Bearer $TOKEN" $B/api/readings/<新id>/interpret & \
curl -s -X POST -H "Authorization: Bearer $TOKEN" $B/api/readings/<同id>/interpret &
wait   # 一邊串流成功、一邊 409；帳本只有一筆 interpret:<id>
```

DB 抽查（Supabase SQL Editor）：`SELECT * FROM credit_transactions ORDER BY created_at DESC LIMIT 10;` 每筆動作都有對應 ledger，`SUM(amount)` = 錢包 balance。

## 5. 禁區
- 不做儲值/金流（M6）、不做訂閱、不做點數包表（env 常數就好）。
- 不改 M1 的 SSE 流程本體，只插入 A/B 兩點。
- 不在前端信任任何點數數字做授權判斷（後端才是真相）。

## 6. 常見坑
- `ExecuteSqlInterpolatedAsync` 的插值是參數化的（安全），**不要**改成字串拼接。
- `DateOnly` 參數若 Npgsql 對映報錯，改傳 `today.ToDateTime(TimeOnly.MinValue).Date` 或字串 `yyyy-MM-dd` 並在 SQL cast `::date`。
- 容器內時區：`Asia/Taipei` 需要 tzdata——.NET 8 官方 image 有內建 ICU/tzdata，本地 `docker compose up` 驗一次 `claim-daily` 的日期切換邏輯即可。
- 帳本 `ON CONFLICT (idempotency_key)`：NULL key 不觸發唯一衝突，所以**每一筆**寫入都必須帶 key（本文件的呼叫都有）。

## 7. Definition of Done
- [ ] 008 已套用（`list_migrations` 可見；四張表存在、RLS enabled）
- [ ] 新戶首查餘額 = 5；每日領取一天一次；解讀扣 1 點且冪等；AI 失敗會退點
- [ ] 併發雙擊只扣一次
- [ ] Navbar 顯示餘額；點數不足有導引
- [ ] `.env.example` 補 M2 變數；`CLAUDE.md` API 表加 balance / claim-daily
- [ ] 跑過 `/qa`
