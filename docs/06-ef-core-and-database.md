# Entity Framework Core 與資料庫 — 給 Node.js 開發者

## 1. ORM 概念對照

EF Core 是 .NET 生態系的主流 ORM，角色等同於 Node.js 世界的 Prisma、Sequelize 或 TypeORM。核心功能：

- 將 C# 類別（class）對應到資料庫表格（table）
- 用 C# 程式碼產生 SQL 查詢（不用手寫 SQL）
- 支援 Migration（資料庫 schema 版本管理）
- 本專案使用 EF Core + **Npgsql** provider 連接 PostgreSQL（Supabase）

| 概念 | EF Core (.NET) | Prisma (Node.js) | Sequelize (Node.js) |
|------|---------------|-------------------|---------------------|
| ORM 實例 | `DbContext` | `PrismaClient` | `Sequelize` instance |
| 表格存取 | `DbSet<T>` | `prisma.model` | `Model.findAll()` |
| 查詢語法 | LINQ | Query API (object) | Method chaining |
| Schema 定義 | Fluent API / Attributes | `schema.prisma` 檔案 | `Model.init()` |
| Migration | `dotnet ef migrations` | `prisma migrate` | `sequelize-cli` |

---

## 2. DbContext — 資料庫連線的核心

`DbContext` 是 EF Core 中與資料庫互動的主要類別，你可以把它想成 Prisma 的 `PrismaClient` 或 Sequelize 的 `sequelize` 實例。

### 本專案的 TarotDbContext

```csharp
// backend/TarotApi/Data/TarotDbContext.cs

public class TarotDbContext : DbContext
{
    public TarotDbContext(DbContextOptions<TarotDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Fluent API 設定（見第 4 節）
    }
}
```

重點：

- **繼承 `DbContext`**：所有的資料庫操作都透過這個類別
- **`DbSet<T>`**：每個 `DbSet` 代表一張表格，等同於 Prisma 的 `prisma.profile` 或 `prisma.reading`
- **`OnModelCreating`**：用來設定表格名稱、欄位對應等（類似 Prisma schema 的 `@@map`）

### 在 Program.cs 中註冊

```csharp
// backend/TarotApi/Program.cs

builder.Services.AddDbContext<TarotDbContext>(options =>
    options.UseNpgsql(dbConnectionString));
```

Node.js 對比：

```javascript
// Prisma
const prisma = new PrismaClient();

// Sequelize
const sequelize = new Sequelize(process.env.DATABASE_URL);
```

.NET 用 **依賴注入（DI）** 管理 `DbContext` 的生命週期。註冊後，任何 Service 或 Controller 都可以在建構子中直接取得 `TarotDbContext`。

---

## 3. Entity 模型定義

Entity 就是對應資料庫表格的 C# 類別，等同於 Prisma 的 `model` 或 Sequelize 的 `Model`。

### Profile Entity

```csharp
// backend/TarotApi/Models/Profile.cs

namespace TarotApi.Models;

public class Profile
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

Prisma 等價寫法：

```prisma
model Profile {
  id          String   @id @default(uuid()) @db.Uuid
  displayName String   @map("display_name")
  createdAt   DateTime @map("created_at") @default(now())
  updatedAt   DateTime @map("updated_at") @updatedAt

  @@map("profiles")
}
```

### Reading Entity（含 JSONB）

```csharp
// backend/TarotApi/Models/Reading.cs

using System.Text.Json;

namespace TarotApi.Models;

public class Reading
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SpreadType { get; set; } = string.Empty;
    public string? Question { get; set; }
    public JsonDocument Cards { get; set; } = null!;  // PostgreSQL JSONB 欄位
    public string? Interpretation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

注意事項：

- **`Guid`** 是 .NET 的 UUID 型別（對應 PostgreSQL 的 `uuid`）
- **`string?`** 中的 `?` 表示 nullable（可為 null），等同於 Prisma 的 `String?`
- **`JsonDocument`** 是 `System.Text.Json` 提供的型別，用來處理 PostgreSQL 的 JSONB 欄位
- **`= string.Empty`** 和 **`= null!`** 是預設值，避免 null reference 警告

---

## 4. Fluent API — 模型與資料庫的對應設定

C# 習慣用 PascalCase（`DisplayName`），但 PostgreSQL 慣用 snake_case（`display_name`）。EF Core 的 Fluent API 讓你在 `OnModelCreating` 裡設定這些對應，概念類似 Prisma schema 的 `@map` 和 `@@map`。

### 本專案的完整設定

```csharp
// backend/TarotApi/Data/TarotDbContext.cs — OnModelCreating 方法

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ── Profile 表格 ──
    modelBuilder.Entity<Profile>(entity =>
    {
        entity.ToTable("profiles");                                    // 表格名稱（@@map）
        entity.HasKey(e => e.Id);                                      // 主鍵（@id）
        entity.Property(e => e.Id).HasColumnName("id");                // 欄位對應（@map）
        entity.Property(e => e.DisplayName).HasColumnName("display_name");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
    });

    // ── Reading 表格 ──
    modelBuilder.Entity<Reading>(entity =>
    {
        entity.ToTable("readings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.UserId).HasColumnName("user_id");
        entity.Property(e => e.SpreadType).HasColumnName("spread_type");
        entity.Property(e => e.Question).HasColumnName("question");
        entity.Property(e => e.Cards)
            .HasColumnName("cards")
            .HasColumnType("jsonb");    // 指定 PostgreSQL 欄位類型為 JSONB
        entity.Property(e => e.Interpretation).HasColumnName("interpretation");
        entity.Property(e => e.Notes).HasColumnName("notes");
        entity.Property(e => e.CreatedAt).HasColumnName("created_at");
    });
}
```

### Fluent API vs Prisma 對照

| Fluent API | Prisma | 用途 |
|-----------|--------|------|
| `entity.ToTable("profiles")` | `@@map("profiles")` | 指定表格名稱 |
| `entity.HasKey(e => e.Id)` | `@id` | 指定主鍵 |
| `entity.Property(e => e.X).HasColumnName("x")` | `@map("x")` | 欄位名稱對應 |
| `.HasColumnType("jsonb")` | `@db.JsonB` | 指定資料庫欄位型別 |

---

## 5. LINQ 查詢 — 資料庫操作

LINQ（Language Integrated Query）是 C# 內建的查詢語法，寫起來像在操作集合（array），EF Core 會把它轉成 SQL。以下全部是本專案的實際程式碼。

### 查詢單筆資料

```csharp
// backend/TarotApi/Services/ProfileService.cs
var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
```

```javascript
// Prisma 等價
const profile = await prisma.profile.findUnique({ where: { id: userId } });
```

`FirstOrDefaultAsync` 找到第一筆符合條件的資料，找不到則回傳 `null`。

### 查詢單筆 + 多條件

```csharp
// backend/TarotApi/Services/ReadingService.cs
var reading = await db.Readings
    .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);
```

```javascript
// Prisma 等價
const reading = await prisma.reading.findFirst({
  where: { id: readingId, userId },
});
```

### 查詢多筆 + 排序 + 分頁

```csharp
// backend/TarotApi/Services/ReadingService.cs — GetReadings 方法
var query = db.Readings
    .Where(r => r.UserId == userId)          // WHERE user_id = ?
    .OrderByDescending(r => r.CreatedAt);    // ORDER BY created_at DESC

var totalCount = await query.CountAsync();   // SELECT COUNT(*)

var readings = await query
    .Skip((page - 1) * pageSize)             // OFFSET
    .Take(pageSize)                          // LIMIT
    .ToListAsync();                          // 執行查詢，轉為 List
```

```javascript
// Prisma 等價
const totalCount = await prisma.reading.count({ where: { userId } });

const readings = await prisma.reading.findMany({
  where: { userId },
  orderBy: { createdAt: 'desc' },
  skip: (page - 1) * pageSize,
  take: pageSize,
});
```

重要觀念：**延遲執行（Deferred Execution）**

LINQ 查詢在呼叫 `ToListAsync()`、`CountAsync()`、`FirstOrDefaultAsync()` 等方法之前，不會實際送出 SQL。這讓你可以逐步組合查詢條件（像組合 query builder），最後一次執行。

### 檢查是否存在

```csharp
// backend/TarotApi/Services/ReadingService.cs — CreateWeeklyFortune 方法
var exists = await db.Readings
    .AnyAsync(r => r.UserId == userId
                   && r.SpreadType == "weekly-fortune"
                   && r.CreatedAt >= weekStart);
```

```javascript
// Prisma 等價
const count = await prisma.reading.count({
  where: { userId, spreadType: 'weekly-fortune', createdAt: { gte: weekStart } },
});
const exists = count > 0;
```

### 分組聚合（GROUP BY）

```csharp
// backend/TarotApi/Services/ReadingService.cs — GetStats 方法
var spreadUsage = await userReadings
    .GroupBy(r => r.SpreadType)                                        // GROUP BY spread_type
    .Select(g => new SpreadStatDto { SpreadType = g.Key, Count = g.Count() })  // SELECT spread_type, COUNT(*)
    .ToListAsync();
```

```javascript
// Prisma 等價
const spreadUsage = await prisma.reading.groupBy({
  by: ['spreadType'],
  where: { userId },
  _count: { _all: true },
});
```

### 新增（INSERT）

```csharp
// backend/TarotApi/Services/ReadingService.cs — CreateReading 方法
var reading = new Reading
{
    Id = Guid.NewGuid(),
    UserId = userId,
    SpreadType = SpreadTypeToString(spreadType),
    Question = question,
    Cards = JsonDocument.Parse(JsonSerializer.Serialize(cardsPayload)),
    CreatedAt = DateTime.UtcNow
};

db.Readings.Add(reading);          // 標記為待新增
await db.SaveChangesAsync();        // 送出 INSERT SQL
```

```javascript
// Prisma 等價
const reading = await prisma.reading.create({
  data: {
    id: crypto.randomUUID(),
    userId,
    spreadType,
    question,
    cards: cardsPayload,
    createdAt: new Date(),
  },
});
```

### 更新（UPDATE）— Change Tracking 自動追蹤

```csharp
// backend/TarotApi/Services/ProfileService.cs — UpdateProfile 方法
var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
if (profile is null) return null;

profile.DisplayName = displayName;       // 直接修改屬性
profile.UpdatedAt = DateTime.UtcNow;
await db.SaveChangesAsync();             // EF Core 自動偵測變更，送出 UPDATE SQL
```

```javascript
// Prisma 等價
const profile = await prisma.profile.update({
  where: { id: userId },
  data: { displayName, updatedAt: new Date() },
});
```

**Change Tracking 是 EF Core 的特色功能**：當你從 `DbContext` 查詢出一個 Entity 後，EF Core 會自動追蹤它的狀態。你只要修改屬性，呼叫 `SaveChangesAsync()` 就會自動產生正確的 `UPDATE` SQL。不需要像 Prisma 那樣明確呼叫 `update()` 方法。

### 刪除（DELETE）

```csharp
// backend/TarotApi/Services/ReadingService.cs — DeleteReading 方法
var reading = await db.Readings
    .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

if (reading is null) return false;

db.Readings.Remove(reading);        // 標記為待刪除
await db.SaveChangesAsync();         // 送出 DELETE SQL
return true;
```

```javascript
// Prisma 等價
await prisma.reading.delete({ where: { id: readingId } });
```

---

## 6. JSONB 處理

PostgreSQL 的 JSONB 型別可以儲存彈性的 JSON 資料。本專案用它來儲存每次抽牌的牌陣資料（卡牌 ID、正逆位、位置），因為不同牌陣的卡牌數量不同，用 JSONB 比建立關聯表更簡單。

### 寫入 JSONB

```csharp
// backend/TarotApi/Services/ReadingService.cs — CreateReading 方法

// 1. 建立要存入的資料結構（匿名型別）
var cardsPayload = drawnCards.Select(dc => new
{
    card_id = dc.Card.Id,
    orientation = dc.Orientation,
    position_index = dc.Position.Index
});

// 2. 序列化成 JSON 字串，再 Parse 成 JsonDocument
Cards = JsonDocument.Parse(JsonSerializer.Serialize(cardsPayload))
```

存入資料庫的 JSONB 長這樣：

```json
[
  { "card_id": "major-0", "orientation": "upright", "position_index": 0 },
  { "card_id": "cups-03", "orientation": "reversed", "position_index": 1 },
  { "card_id": "swords-07", "orientation": "upright", "position_index": 2 }
]
```

### 讀取 JSONB

```csharp
// backend/TarotApi/Services/ReadingService.cs — ResolveCards 方法

var cardsJson = reading.Cards.RootElement;    // 取得 JSON 根元素

foreach (var element in cardsJson.EnumerateArray())   // 遍歷 JSON 陣列
{
    var cardId = element.GetProperty("card_id").GetString()!;
    var orientation = element.GetProperty("orientation").GetString()!;
    var positionIndex = element.GetProperty("position_index").GetInt32();

    // 用 cardId 去靜態資料查找完整的卡牌資訊
    var card = TarotCards.GetById(cardId);
    // ...
}
```

### Node.js 對比

在 Node.js 中，JSON 是原生支援的，所以處理 JSONB 非常自然：

```javascript
// Prisma 直接就是 JavaScript object
const cards = reading.cards; // 已經是 array of objects
cards.forEach((card) => {
  console.log(card.card_id);
});
```

C# 需要透過 `JsonDocument` / `JsonElement` API 來操作 JSON，雖然比較囉嗦，但提供了型別安全的存取方式。

---

## 7. Raw SQL 查詢

當 LINQ 無法表達複雜查詢時（例如 PostgreSQL 特有的 JSONB 函式），可以用 Raw SQL。本專案在統計「最常出現的卡牌」時就用到了：

```csharp
// backend/TarotApi/Services/ReadingService.cs — GetStats 方法

var topCards = await db.Database
    .SqlQueryRaw<CardStatRaw>(
        """
        SELECT card->>'card_id' AS CardId, COUNT(*) AS Count
        FROM readings, jsonb_array_elements(cards) AS card
        WHERE user_id = {0}
        GROUP BY card->>'card_id'
        ORDER BY Count DESC
        LIMIT 5
        """, userId)
    .ToListAsync();
```

SQL 拆解：

| SQL 片段 | 說明 |
|----------|------|
| `jsonb_array_elements(cards)` | 把 JSONB 陣列展開成多行（PostgreSQL 專用函式） |
| `card->>'card_id'` | 從每個 JSON 物件取出 `card_id` 欄位的文字值 |
| `{0}` | 參數化查詢，EF Core 會自動帶入 `userId`（防 SQL injection） |

結果會映射到這個 C# 類別：

```csharp
private class CardStatRaw
{
    public string CardId { get; set; } = string.Empty;
    public int Count { get; set; }
}
```

### Node.js 對比

```javascript
// Prisma — $queryRaw
const topCards = await prisma.$queryRaw`
  SELECT card->>'card_id' AS "cardId", COUNT(*)::int AS "count"
  FROM readings, jsonb_array_elements(cards) AS card
  WHERE user_id = ${userId}::uuid
  GROUP BY card->>'card_id'
  ORDER BY "count" DESC
  LIMIT 5
`;

// Sequelize — sequelize.query
const [topCards] = await sequelize.query(
  `SELECT card->>'card_id' AS "cardId", COUNT(*) AS "count"
   FROM readings, jsonb_array_elements(cards) AS card
   WHERE user_id = $1
   GROUP BY card->>'card_id'
   ORDER BY "count" DESC
   LIMIT 5`,
  { bind: [userId] }
);
```

---

## 8. Service 中使用 DbContext 的模式

本專案透過 **Primary Constructor**（C# 12 語法）注入 `TarotDbContext`：

```csharp
// backend/TarotApi/Services/ProfileService.cs
public class ProfileService(TarotDbContext db)
{
    public async Task<ProfileDto?> GetProfile(Guid userId)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
        // ...
    }
}

// backend/TarotApi/Services/ReadingService.cs
public class ReadingService(TarotDbContext db, TarotService tarotService)
{
    public async Task<ReadingResponseDto> CreateReading(Guid userId, SpreadType spreadType, string? question)
    {
        // 使用 db 和 tarotService ...
    }
}
```

Node.js 的慣用模式通常不需要 DI：

```javascript
// Node.js — 直接 import
import { prisma } from '../db.js';

export async function getProfile(userId) {
  return prisma.profile.findUnique({ where: { id: userId } });
}
```

.NET 使用 DI 的好處是方便抽換實作（例如測試時可以 mock `DbContext`），而且 `DbContext` 的生命週期由框架管理（每個 HTTP request 一個實例）。

---

## 9. SaveChangesAsync — Unit of Work 模式

`SaveChangesAsync()` 是 EF Core 中的關鍵概念。它實現了 **Unit of Work** 模式：所有的變更（新增、修改、刪除）都先在記憶體中標記，直到呼叫 `SaveChangesAsync()` 才會一次性送出到資料庫。

```csharp
// 可以做多個變更，最後一次存檔
db.Readings.Add(reading1);
db.Readings.Add(reading2);
db.Readings.Remove(oldReading);
await db.SaveChangesAsync();  // 一次送出 2 個 INSERT + 1 個 DELETE（在同一個 transaction 裡）
```

Node.js 對比（Prisma transaction）：

```javascript
await prisma.$transaction([
  prisma.reading.create({ data: reading1 }),
  prisma.reading.create({ data: reading2 }),
  prisma.reading.delete({ where: { id: oldReadingId } }),
]);
```

---

## 10. 完整比較表

| 功能 | EF Core | Prisma | Sequelize |
|------|---------|--------|-----------|
| 查詢語言 | LINQ（C# 原生語法） | Query API（物件） | Method chaining |
| Change Tracking | 內建，自動追蹤修改 | 無（需明確呼叫 update） | Instance methods |
| 延遲執行 | 有（ToListAsync 才執行）| 無（每次呼叫即執行） | 無 |
| Migration | `dotnet ef migrations add` | `prisma migrate dev` | `sequelize db:migrate` |
| Raw SQL | `SqlQueryRaw<T>` | `$queryRaw` | `sequelize.query` |
| JSONB 支援 | `JsonDocument` + `HasColumnType("jsonb")` | `Json` 型別 | `Sequelize.JSON` |
| 交易（Transaction） | `SaveChangesAsync()` 自動 | `$transaction` | `sequelize.transaction` |
| 連線管理 | DI 注入，per-request | 單例 Client | 單例 instance |
| Schema 定義位置 | Fluent API 或 Attributes | `.prisma` 檔案 | Model 定義檔 |
