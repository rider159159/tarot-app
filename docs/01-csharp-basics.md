# C# 語言基礎 — 給 Node.js 開發者

這份文件是為有 Node.js/TypeScript 後端經驗的開發者撰寫的 C# 入門指南。所有範例皆取自本專案 (Tarot App) 的實際程式碼，讓你能直接對照學習。

---

## 1. 型別系統

C# 是**強型別、靜態型別**語言。這代表每個變數在編譯時期就必須確定型別，且不允許隱式轉換不相容的型別。相較之下，JavaScript 是動態型別（runtime 才決定型別），TypeScript 則是可選的靜態型別（編譯後型別資訊消失）。

C# 的型別錯誤會在編譯期被攔截，不會等到執行時才爆炸，這是它最大的優勢。

### 常見型別對照表

| C# 型別 | JS/TS 對應 | 說明 |
|---------|-----------|------|
| `int` | `number` | 32 位元整數 |
| `long` | `number` / `bigint` | 64 位元整數 |
| `double` | `number` | 64 位元浮點數（最接近 JS number） |
| `bool` | `boolean` | 布林值 |
| `string` | `string` | 字串（C# 是不可變的參考型別） |
| `Guid` | `string` (UUID) | 128 位元唯一識別碼，內建型別 |
| `DateTime` | `Date` | 日期時間 |
| `List<T>` | `T[]` / `Array<T>` | 可變長度泛型集合 |
| `Dictionary<K,V>` | `Map<K,V>` / `Record<K,V>` | 鍵值對集合 |
| `T[]` | `T[]` | 固定長度陣列 |
| `Task<T>` | `Promise<T>` | 非同步操作的回傳型別 |
| `T?` | `T \| null` / `T \| undefined` | 可為 null 的型別 |

在本專案中，你會大量看到 `Guid`（使用者 ID、牌陣 ID）和 `List<T>`（抽牌結果）。

---

## 2. 變數宣告

C# 的 `var` 跟 TypeScript 的 `let` 很像——編譯器會自動推斷型別。但 C# 沒有 `let` vs `const` 的區分；`var` 宣告的區域變數都是可重新賦值的。

### 並排比較

```csharp
// C# — 明確型別宣告
string name = "塔羅牌";
int count = 78;
bool isReversed = false;
Guid userId = Guid.NewGuid();

// C# — var 自動推斷（推薦寫法，型別從右側推斷）
var name = "塔羅牌";         // 推斷為 string
var count = 78;              // 推斷為 int
var isReversed = false;      // 推斷為 bool
var userId = Guid.NewGuid(); // 推斷為 Guid
```

```typescript
// TypeScript
const name: string = "塔羅牌";
let count: number = 78;
const isReversed: boolean = false;
const userId: string = crypto.randomUUID();
```

### const vs readonly

C# 的 `const` 只能用於編譯期常數（數字、字串、布林），跟 JS 的 `const` 概念不同。`readonly` 則更接近 JS 的 `const`——初始化後不可重新賦值。

```csharp
// C# — const 必須是編譯期確定的值
const int MaxCards = 78;
const string AppName = "Tarot";

// C# — readonly 可以在建構式中設定，之後不可變
private static readonly Dictionary<SpreadType, SpreadPosition[]> SpreadConfigs = new()
{
    // ... 本專案 TarotService.cs 中的牌陣設定
};
```

```typescript
// TypeScript — const 阻止重新賦值，但物件內容可變
const MAX_CARDS = 78;
const SPREAD_CONFIGS = { /* ... */ } as const; // as const 才能凍結內容
```

### 字串插值

兩者語法幾乎一樣，只是符號不同：

```csharp
// C# — 用 $ 開頭、{} 包變數
var message = $"用戶 {userId} 抽了 {count} 張牌";
var url = $"{supabaseUrl}/auth/v1";  // 本專案 Program.cs
```

```typescript
// TypeScript — 用反引號、${} 包變數
const message = `用戶 ${userId} 抽了 ${count} 張牌`;
const url = `${supabaseUrl}/auth/v1`;
```

---

## 3. Nullable 型別

C# 8 引入了 **Nullable Reference Types**，跟 TypeScript 的 strict null checks 是同一個概念：強制你處理 null 的可能性。在型別後面加 `?` 表示該值可能是 null。

```csharp
// C# — 本專案 Reading.cs
public string? Question { get; set; }      // 可為 null
public string SpreadType { get; set; }     // 不可為 null（編譯器會警告）
```

```typescript
// TypeScript
question?: string;      // 可為 undefined
question: string | null; // 可為 null
```

### ?? 運算子 (Null Coalescing)

C# 和 JS 都有 `??`，行為完全一致——左邊是 null 時取右邊的值：

```csharp
// C# — 本專案 Program.cs 中的實際程式碼
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173"];

// ?? 也能搭配 throw（本專案大量使用的模式）
var jwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET")
    ?? throw new InvalidOperationException("SUPABASE_JWT_SECRET is not set");
```

```typescript
// TypeScript
const allowedOrigins = process.env.ALLOWED_ORIGINS?.split(',') ?? ['http://localhost:5173'];

// TS 中沒有 ?? throw，需要另外處理
const jwtSecret = process.env.SUPABASE_JWT_SECRET;
if (!jwtSecret) throw new Error('SUPABASE_JWT_SECRET is not set');
```

### ?. 運算子 (Null Conditional)

跟 JS 的 optional chaining 一模一樣：

```csharp
// C# — 本專案 ReadingService.cs
var cardInfo = TarotCards.GetById(tc.CardId);
var name = cardInfo?.NameCht ?? tc.CardId;  // 如果 cardInfo 是 null，就用 CardId
```

```typescript
// TypeScript
const cardInfo = TarotCards.getById(tc.cardId);
const name = cardInfo?.nameCht ?? tc.cardId;
```

`?? throw` 這個模式是 C# 獨有的語法糖，在本專案的 `Program.cs` 中被大量使用來驗證環境變數。這比 TypeScript 需要額外寫 if 判斷要簡潔許多。

---

## 4. 類別、介面、存取修飾詞

C# 的 class 和 interface 概念與 TypeScript 相似，但有更嚴格的存取控制機制。

### class vs interface

```csharp
// C# — interface 定義契約（只有方法簽章，沒有實作）
public interface ITarotService
{
    List<DrawnCardResult> DrawCards(SpreadType spreadType);
}

// C# — class 實作介面
public class TarotService : ITarotService  // 用 : 繼承/實作
{
    public List<DrawnCardResult> DrawCards(SpreadType spreadType)
    {
        // 實作...
    }
}
```

```typescript
// TypeScript
interface ITarotService {
    drawCards(spreadType: SpreadType): DrawnCardResult[];
}

class TarotService implements ITarotService {
    drawCards(spreadType: SpreadType): DrawnCardResult[] {
        // 實作...
    }
}
```

### 存取修飾詞

| 修飾詞 | C# | TS/JS 對應 | 說明 |
|--------|-----|-----------|------|
| `public` | 任何地方都能存取 | `public`（TS） | 預設在 TS class 中 |
| `private` | 只有同一個 class 內 | `private` / `#field` | C# class 成員預設是 private |
| `protected` | 同 class 及子類別 | `protected` | 相同概念 |
| `internal` | 同一個組件 (Assembly) 內 | **無對應** | C# 獨有，同一個專案內可存取 |

`internal` 是 C# 特色——在同一個 .NET 專案（如 `TarotApi`）內的所有檔案都能存取，但外部專案不行。適合用在「不想公開但同專案需要共用」的場景。

### 命名慣例

```csharp
// C# 慣例
public class ReadingService                 // PascalCase: 類別名
{
    private readonly TarotDbContext _db;     // _camelCase: private 欄位（底線開頭）

    public async Task<bool> DeleteReading() // PascalCase: 方法名
    {
        var totalCount = 0;                 // camelCase: 區域變數
    }
}
```

```typescript
// TypeScript/JS 慣例
class ReadingService {                      // PascalCase: 類別名
    private db: TarotDbContext;             // camelCase: private 欄位（無底線）

    async deleteReading(): Promise<boolean> // camelCase: 方法名
    {
        const totalCount = 0;              // camelCase: 區域變數
    }
}
```

最大差異：C# 的方法名用 **PascalCase**（`DeleteReading`），JS/TS 用 **camelCase**（`deleteReading`）。

---

## 5. 屬性 (Properties)

C# 的 Properties 是語言內建的 getter/setter 語法糖，比 JS 的 `get`/`set` 更簡潔，而且是 C# 中定義資料模型的主要方式。

### Auto Properties

```csharp
// C# — 本專案 Profile.cs 的完整程式碼
public class Profile
{
    public Guid Id { get; set; }                              // 可讀可寫
    public string DisplayName { get; set; } = string.Empty;   // 預設值為空字串
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

```typescript
// TypeScript 等價寫法
class Profile {
    id: string = '';           // Guid 在 TS 中通常用 string
    displayName: string = '';
    createdAt: Date = new Date();
    updatedAt: Date = new Date();
}
```

### init-only 屬性

`{ get; init; }` 表示只能在物件初始化時設定，之後不可修改。本專案的 `ErrorResponseDto` 使用了這個模式：

```csharp
// C# — 本專案 ErrorResponseDto.cs
public record ErrorResponseDto
{
    public string Error { get; init; } = string.Empty;  // 初始化後不可變
    public string? Code { get; init; }                   // 可為 null，初始化後不可變
}

// 使用方式：
var error = new ErrorResponseDto { Error = "找不到牌陣", Code = "NOT_FOUND" };
// error.Error = "其他錯誤";  // 編譯錯誤！init-only 不可重新賦值
```

```typescript
// TypeScript 等價寫法
interface ErrorResponseDto {
    readonly error: string;
    readonly code?: string;
}
```

### 預設值 `= string.Empty`

你會在本專案的 DTO 中大量看到 `= string.Empty;` 或 `= [];`。這是為了滿足 C# 的 nullable reference types 檢查——告訴編譯器「這個欄位保證不會是 null」。

```csharp
// 本專案 ReadingResponseDto.cs
public class ReadingResponseDto
{
    public string SpreadType { get; set; } = string.Empty;  // 保證非 null
    public string? Question { get; set; }                    // 明確標示可為 null
    public List<DrawnCardDto> Cards { get; set; } = [];      // 預設空陣列
}
```

---

## 6. Record 型別

Record 是 C# 9 引入的特殊型別，專為**不可變的資料載體**設計。它自動幫你生成 `Equals()`、`GetHashCode()`、`ToString()` 等方法，非常適合 DTO 和值物件。

### Positional Record（位置參數記錄）

本專案的 `SpreadPosition` 就是 positional record 的經典用法：

```csharp
// C# — 本專案 SpreadPosition.cs（整個檔案就這一行！）
public record SpreadPosition(int Index, string Label, string Description);

// 等同於一個有三個唯讀屬性的不可變類別
// 使用方式（本專案 TarotService.cs）：
new SpreadPosition(0, "指引", "此刻對你最重要的訊息")

// 解構 (Deconstruction)：
var (index, label, desc) = somePosition;
```

```typescript
// TypeScript 等價寫法需要更多程式碼
interface SpreadPosition {
    readonly index: number;
    readonly label: string;
    readonly description: string;
}
// 且沒有內建的值相等比較
```

### Record with Properties

本專案的 `ErrorResponseDto` 使用了帶屬性的 record：

```csharp
// C# — 本專案 ErrorResponseDto.cs
public record ErrorResponseDto
{
    public string Error { get; init; } = string.Empty;
    public string? Code { get; init; }
}
```

### Record vs Class 差異

| 特性 | `record` | `class` |
|------|----------|---------|
| 相等比較 | 值相等（比內容） | 參考相等（比記憶體位址） |
| 可變性 | 預設不可變（init） | 預設可變（set） |
| `ToString()` | 自動生成含所有屬性 | 預設只有型別名稱 |
| 適用場景 | DTO、值物件、設定 | 有狀態的服務、實體 |

```csharp
// 值相等示範
var pos1 = new SpreadPosition(0, "指引", "訊息");
var pos2 = new SpreadPosition(0, "指引", "訊息");
Console.WriteLine(pos1 == pos2); // true（record 比內容）

// 如果是 class：
// pos1 == pos2 → false（class 比記憶體位址）
```

在 TypeScript 中要達成值相等，你需要用 `Object.freeze()` 加上自己寫的比較邏輯。C# 的 record 把這些全部內建了。

---

## 7. 列舉 (Enum)

C# 的 enum 是真正的型別（有編譯期保證），比 TypeScript 的 enum 更嚴格也更安全。

### 本專案的 SpreadType 列舉

```csharp
// C# — 本專案 SpreadType.cs
using System.Text.Json.Serialization;

namespace TarotApi.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]  // JSON 序列化時用字串而非數字
public enum SpreadType
{
    Single,            // 0（預設從 0 開始）
    ThreeCardTime,     // 1
    ThreeCardProblem,  // 2
    ThreeCardLinear,   // 3
    CelticCross,       // 4
    WeeklyFortune      // 5
}
```

```typescript
// TypeScript — 相似寫法
enum SpreadType {
    Single = 'Single',
    ThreeCardTime = 'ThreeCardTime',
    ThreeCardProblem = 'ThreeCardProblem',
    ThreeCardLinear = 'ThreeCardLinear',
    CelticCross = 'CelticCross',
    WeeklyFortune = 'WeeklyFortune',
}
// 或更常見的寫法：
type SpreadType = 'single' | 'three-card-time' | 'three-card-problem' | ...;
```

### [JsonConverter] 屬性

C# enum 預設序列化為數字（`0`, `1`, `2`...），加上 `[JsonConverter(typeof(JsonStringEnumConverter))]` 後會序列化為字串（`"Single"`, `"ThreeCardTime"`）。這個 attribute（屬性標注）的概念類似 TypeScript 的 decorator，但在 C# 中是編譯期處理的。

### Enum 作為 Dictionary Key

```csharp
// C# — 本專案 TarotService.cs，enum 直接當 Dictionary 的 key
private static readonly Dictionary<SpreadType, SpreadPosition[]> SpreadConfigs = new()
{
    [SpreadType.Single] = [ new(0, "指引", "此刻對你最重要的訊息") ],
    [SpreadType.ThreeCardTime] = [ /* ... */ ],
    // ...
};
```

```typescript
// TypeScript — 用 Record 型別
const spreadConfigs: Record<SpreadType, SpreadPosition[]> = {
    [SpreadType.Single]: [{ index: 0, label: '指引', description: '此刻對你最重要的訊息' }],
    // ...
};
```

C# enum 的優勢在於它是獨立的型別，不會像 TS 的 string union 在 runtime 消失。

---

## 8. async/await

好消息：C# 的 async/await 概念與 JavaScript **幾乎完全相同**。如果你熟悉 JS 的 Promise，學 C# 的 Task 會非常輕鬆。

### 核心對應

| C# | TypeScript | 說明 |
|----|-----------|------|
| `Task<T>` | `Promise<T>` | 非同步回傳值 |
| `Task` | `Promise<void>` | 無回傳值的非同步操作 |
| `await` | `await` | 等待非同步完成 |
| `async` | `async` | 標記非同步方法 |

### 並排比較

```csharp
// C# — 本專案 ProfileService.cs
public async Task<ProfileDto?> GetProfile(Guid userId)
{
    var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
    if (profile is null) return null;

    return new ProfileDto
    {
        Id = profile.Id,
        DisplayName = profile.DisplayName,
        CreatedAt = profile.CreatedAt
    };
}
```

```typescript
// TypeScript 等價寫法
async function getProfile(userId: string): Promise<ProfileDto | null> {
    const profile = await db.profiles.findFirst({ where: { id: userId } });
    if (profile === null) return null;

    return {
        id: profile.id,
        displayName: profile.displayName,
        createdAt: profile.createdAt,
    };
}
```

### 關鍵差異

1. **C# async 方法一定回傳 Task**：不像 JS 可以回傳原始值，C# 的 async 方法回傳型別必須是 `Task`、`Task<T>` 或 `ValueTask<T>`。

2. **命名慣例**：C# 的非同步方法通常以 `Async` 結尾（如 `SaveChangesAsync()`、`FirstOrDefaultAsync()`），但你自己的方法不一定要遵循這個慣例。

3. **不需要 `Promise.all()`**：C# 用 `Task.WhenAll()` 達成同樣效果。

```csharp
// C# — 平行執行多個非同步操作
var totalCount = await query.CountAsync();  // 依序執行

// 平行執行：
var (count, readings) = (
    await Task.WhenAll(query.CountAsync(), query.ToListAsync())
);
```

```typescript
// TypeScript
const [count, readings] = await Promise.all([
    query.count(),
    query.findMany()
]);
```

---

## 9. LINQ — C# 的 Array Methods

LINQ (Language Integrated Query) 是 C# 最強大的特性之一。如果你熟悉 JavaScript 的 Array methods（`map`, `filter`, `reduce`），LINQ 就是它的強化版，而且可以直接操作資料庫查詢。

### 方法對照表

| C# LINQ | JS Array Method | 說明 |
|---------|----------------|------|
| `.Where(x => ...)` | `.filter(x => ...)` | 篩選元素 |
| `.Select(x => ...)` | `.map(x => ...)` | 轉換元素 |
| `.FirstOrDefault(x => ...)` | `.find(x => ...)` | 找第一個符合的元素 |
| `.Any(x => ...)` | `.some(x => ...)` | 是否有任何元素符合 |
| `.All(x => ...)` | `.every(x => ...)` | 是否所有元素都符合 |
| `.Count()` | `.length` | 元素數量 |
| `.OrderBy(x => ...)` | `.sort((a,b) => ...)` | 排序（不改變原集合） |
| `.OrderByDescending(x => ...)` | `.sort((a,b) => ...).reverse()` | 反向排序 |
| `.GroupBy(x => ...)` | 手動 `reduce()` | 分組 |
| `.Skip(n).Take(m)` | `.slice(n, n+m)` | 跳過/取得 |
| `.ToList()` | `[...iterable]` | 強制執行並轉為 List |

### 本專案實際範例

```csharp
// 本專案 ReadingService.cs — 分頁查詢（連鎖呼叫風格跟 JS 很像）
var query = db.Readings
    .Where(r => r.UserId == userId)           // .filter()
    .OrderByDescending(r => r.CreatedAt);     // .sort() 反向

var readings = await query
    .Skip((page - 1) * pageSize)              // .slice() 起點
    .Take(pageSize)                           // .slice() 長度
    .ToListAsync();                           // 執行查詢，轉為 List

// 轉換結果（.Select = .map）
var items = readings.Select(r => ToResponseDto(r, ResolveCards(r))).ToList();
```

```typescript
// TypeScript 等價寫法
const readings = await db.readings
    .filter(r => r.userId === userId)
    .sort((a, b) => b.createdAt - a.createdAt)
    .slice((page - 1) * pageSize, page * pageSize);

const items = readings.map(r => toResponseDto(r, resolveCards(r)));
```

### LINQ 的超能力：直接操作資料庫

LINQ 最厲害的地方在於——當搭配 EF Core 使用時，它會自動把 C# 程式碼轉譯成 SQL 查詢：

```csharp
// 本專案 ReadingService.cs — 這段 C# 會自動變成 SQL
var spreadUsage = await db.Readings
    .Where(r => r.UserId == userId)
    .GroupBy(r => r.SpreadType)
    .Select(g => new SpreadStatDto { SpreadType = g.Key, Count = g.Count() })
    .ToListAsync();
// 實際執行的是 SQL: SELECT spread_type, COUNT(*) FROM readings WHERE user_id = @p0 GROUP BY spread_type
```

在 TypeScript 中，你需要 ORM（如 Prisma、Drizzle）來達成類似效果，但 LINQ 的整合度更高，因為它是語言內建的。

---

## 10. 命名空間 (Namespace) vs ES Modules

C# 用 **namespace** 組織程式碼，而 JS/TS 用 **ES Modules**（import/export）。兩者目的相同：避免命名衝突、組織程式結構。

### File-scoped Namespace（本專案使用的風格）

```csharp
// C# — 本專案所有檔案都用 file-scoped namespace（C# 10+，少一層縮排）
namespace TarotApi.Services;  // 分號結尾，整個檔案都屬於這個 namespace

public class ReadingService
{
    // ...
}
```

```csharp
// C# — 傳統寫法（多一層大括號，現在較少用）
namespace TarotApi.Services
{
    public class ReadingService
    {
        // ...
    }
}
```

### using vs import

```csharp
// C# — 本專案 ReadingService.cs 的 using 宣告
using System.Text.Json;               // 引入 .NET 內建函式庫
using Microsoft.EntityFrameworkCore;   // 引入 NuGet 套件
using TarotApi.Data;                   // 引入專案內的其他 namespace
using TarotApi.Models;
using TarotApi.Models.Dtos;
```

```typescript
// TypeScript
import { JsonDocument } from 'system/text-json';   // （假想的對應）
import { db } from '../data/context';               // 引入專案內的模組
import { Reading } from '../models/reading';
import type { ReadingResponseDto } from '../models/dtos';
```

### 關鍵差異

| 面向 | C# namespace | ES Modules |
|------|-------------|------------|
| 匯出 | `public` class 自動對外可見 | 必須明確 `export` |
| 匯入 | `using` 匯入整個 namespace | `import` 匯入特定項目 |
| 檔案關係 | 一個 namespace 可跨多個檔案 | 一個檔案就是一個模組 |
| 預設行為 | 沒有 `using` 就無法使用 | 沒有 `import` 就無法使用 |

C# 不需要像 JS 那樣在每個檔案寫 `export`，只要 class 是 `public` 的，其他檔案加上 `using` 就能用。

---

## 11. 集合初始化語法

C# 提供了多種簡潔的語法來建立集合，隨著版本更新越來越接近 JS 的簡潔風格。

### `new() { ... }` 物件初始化

```csharp
// C# — 本專案 ReadingService.cs 中建立 Reading 物件
var reading = new Reading
{
    Id = Guid.NewGuid(),
    UserId = userId,
    SpreadType = SpreadTypeToString(spreadType),
    Question = question,
    Cards = JsonDocument.Parse(JsonSerializer.Serialize(cardsPayload)),
    CreatedAt = DateTime.UtcNow
};
```

```typescript
// TypeScript — 直接用物件字面值
const reading = {
    id: crypto.randomUUID(),
    userId,
    spreadType: spreadTypeToString(spreadType),
    question,
    cards: cardsPayload,
    createdAt: new Date(),
};
```

### Collection Expression `[]`（C# 12 新語法）

本專案在多處使用了 C# 12 的集合表達式 `[]`，語法跟 JS 陣列一模一樣：

```csharp
// C# 12 — 本專案中的實際用法
public List<DrawnCardDto> Cards { get; set; } = [];              // 空 List
public string[] Keywords { get; set; } = [];                     // 空陣列
var allowedOrigins = ... ?? ["http://localhost:5173"];            // 內含元素的陣列

// 搭配 SpreadPosition（本專案 TarotService.cs）
[SpreadType.Single] = [
    new(0, "指引", "此刻對你最重要的訊息")
],
```

```typescript
// TypeScript — 幾乎一樣
cards: DrawnCardDto[] = [];
keywords: string[] = [];
const allowedOrigins = ... ?? ['http://localhost:5173'];
```

### Dictionary 初始化

```csharp
// C# — 本專案 TarotService.cs 的 Dictionary 初始化
private static readonly Dictionary<SpreadType, SpreadPosition[]> SpreadConfigs = new()
{
    [SpreadType.Single] = [ new(0, "指引", "此刻對你最重要的訊息") ],
    [SpreadType.ThreeCardTime] = [
        new(0, "過去", "影響當前情況的過去因素"),
        new(1, "現在", "目前的狀態與挑戰"),
        new(2, "未來", "如果沿著目前道路前進的可能發展")
    ],
    // ...
};
```

```typescript
// TypeScript — 用 Map 或物件
const spreadConfigs = new Map<SpreadType, SpreadPosition[]>([
    [SpreadType.Single, [{ index: 0, label: '指引', description: '此刻對你最重要的訊息' }]],
    [SpreadType.ThreeCardTime, [
        { index: 0, label: '過去', description: '影響當前情況的過去因素' },
        { index: 1, label: '現在', description: '目前的狀態與挑戰' },
        { index: 2, label: '未來', description: '如果沿著目前道路前進的可能發展' },
    ]],
]);
```

C# 的 `new()` 是 target-typed new（從左側推斷型別），加上 `[]` 集合表達式，讓初始化語法變得非常簡潔。

---

## 12. Pattern Matching & Switch Expression

C# 的 Pattern Matching 是相當強大的功能，讓你用更宣告式的方式處理條件邏輯。本專案在多處使用了 switch expression 和 `is` 模式。

### Switch Expression

傳統 switch 語句在 C# 中可以用 **switch expression** 簡化，用 `=>` 取代 `case`/`break`：

```csharp
// C# — 本專案 ReadingService.cs 的實際程式碼
private static string SpreadTypeToString(SpreadType type) => type switch
{
    SpreadType.Single => "single",
    SpreadType.ThreeCardTime => "three-card-time",
    SpreadType.ThreeCardProblem => "three-card-problem",
    SpreadType.ThreeCardLinear => "three-card-linear",
    SpreadType.CelticCross => "celtic-cross",
    SpreadType.WeeklyFortune => "weekly-fortune",
    _ => throw new ArgumentOutOfRangeException(nameof(type))  // _ 是預設（類似 default）
};
```

```typescript
// TypeScript — 通常用物件映射或 switch
function spreadTypeToString(type: SpreadType): string {
    const map: Record<SpreadType, string> = {
        Single: 'single',
        ThreeCardTime: 'three-card-time',
        ThreeCardProblem: 'three-card-problem',
        ThreeCardLinear: 'three-card-linear',
        CelticCross: 'celtic-cross',
        WeeklyFortune: 'weekly-fortune',
    };
    return map[type] ?? (() => { throw new Error(`Unknown type: ${type}`) })();
}
```

C# 的 switch expression 更簡潔，而且編譯器會警告你是否漏掉了 enum 的某個值（exhaustiveness check）。

### `is` Pattern（空值檢查）

本專案大量使用 `is null` 和 `is not null` 進行空值檢查，比 `== null` 更安全（不受運算子覆寫影響）：

```csharp
// C# — 本專案 ReadingService.cs
var reading = await db.Readings.FirstOrDefaultAsync(r => r.Id == readingId);

if (reading is null) return false;          // is null 模式
// 等價但較不推薦：if (reading == null)

return reading is null ? null : ToResponseDto(reading, ResolveCards(reading));

// 本專案 ResolveCards 中：
var card = TarotCards.GetById(cardId);
if (card is null) continue;                 // 跳過找不到的牌
```

```typescript
// TypeScript — 用 === null 或可選鍵
if (reading === null) return false;
if (!card) continue;
```

### 反向字串解析也用 switch expression

```csharp
// 本專案 ReadingService.cs — 字串轉回 enum
var spreadType = reading.SpreadType switch
{
    "single" => SpreadType.Single,
    "three-card" => SpreadType.ThreeCardTime,
    "three-card-time" => SpreadType.ThreeCardTime,
    "three-card-problem" => SpreadType.ThreeCardProblem,
    "three-card-linear" => SpreadType.ThreeCardLinear,
    "celtic-cross" => SpreadType.CelticCross,
    "weekly-fortune" => SpreadType.WeeklyFortune,
    _ => SpreadType.Single   // 預設值
};
```

Switch expression 的優勢在於它是**表達式**（有回傳值），可以直接賦值給變數，不需要在每個 case 裡寫 `return` 或 `break`。這讓程式碼更簡潔也更不容易出錯。

---

## 小結

如果你已經熟悉 TypeScript，學習 C# 的門檻其實不高。兩者在語法層面有大量相似之處：

- **幾乎一樣的**：async/await、?? 運算子、?. 運算子、字串插值、class/interface
- **概念相同但語法不同的**：LINQ vs Array methods、namespace vs modules、Properties vs getter/setter
- **C# 獨有的**：record 值相等、`is` pattern matching、switch expression、`internal` 存取修飾詞、`{ get; init; }` 唯讀屬性

接下來可以直接閱讀本專案的 `backend/TarotApi/` 原始碼，搭配這份文件作為速查參考。
