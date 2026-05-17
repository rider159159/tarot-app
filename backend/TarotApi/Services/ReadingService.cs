using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TarotApi.Data;
using TarotApi.Models;
using TarotApi.Models.Dtos;

namespace TarotApi.Services;

public class ReadingService(TarotDbContext db, TarotService tarotService)
{
    public async Task<ReadingResponseDto> CreateReading(Guid userId, SpreadType spreadType, string? question, int customCardCount = 0)
    {
        var drawnCards = tarotService.DrawCards(spreadType, customCardCount);

        // Build the JSONB payload for DB storage (compact format)
        var cardsPayload = drawnCards.Select(dc => new
        {
            card_id = dc.Card.Id,
            orientation = dc.Orientation,
            position_index = dc.Position.Index
        });

        var reading = new Reading
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SpreadType = SpreadTypeToString(spreadType),
            Question = question,
            Cards = JsonDocument.Parse(JsonSerializer.Serialize(cardsPayload)),
            CreatedAt = DateTime.UtcNow
        };

        db.Readings.Add(reading);
        await db.SaveChangesAsync();

        return ToResponseDto(reading, drawnCards);
    }

    // Anonymous draw — no DB write. Result lives only in the HTTP response;
    // the client persists it in localStorage along with a self-generated
    // clientToken and may later POST it to /api/readings/import.
    public AnonymousDrawResponseDto DrawAnonymous(SpreadType spreadType, string? question, int customCardCount = 0)
    {
        if (spreadType == SpreadType.WeeklyFortune)
            throw new ReadingImportException("週運需要登入才能抽", "AUTH_REQUIRED");

        var drawnCards = tarotService.DrawCards(spreadType, customCardCount);
        return new AnonymousDrawResponseDto
        {
            SpreadType = SpreadTypeToString(spreadType),
            Question = question,
            DrawnAt = DateTime.UtcNow,
            Cards = drawnCards.Select(MapDrawnCardToDto).ToList()
        };
    }

    // Import a previously-drawn anonymous reading after login.
    // Idempotent on client_token: re-importing returns the existing reading.
    public async Task<ReadingResponseDto> ImportReading(Guid userId, ReadingImportDto dto)
    {
        // Idempotency check
        var existing = await db.Readings
            .FirstOrDefaultAsync(r => r.ClientToken == dto.ClientToken);
        if (existing is not null)
        {
            if (existing.UserId != userId)
                throw new ReadingImportException("此抽牌紀錄已被其他帳號保存", "ALREADY_IMPORTED_BY_OTHER");
            return ToResponseDto(existing, ResolveCards(existing));
        }

        var spreadType = ParseImportableSpread(dto.SpreadType);
        if (spreadType == SpreadType.Custom)
        {
            if (dto.Cards.Count is < 1 or > 10)
                throw new ReadingImportException(
                    $"自定義牌陣張數需介於 1～10（實際 {dto.Cards.Count}）",
                    "INVALID_CARD_COUNT");
        }
        else
        {
            var positions = TarotService.GetPositions(spreadType);
            var expectedCount = spreadType == SpreadType.Single ? positions.Length : positions.Length + 1;
            if (dto.Cards.Count != expectedCount)
                throw new ReadingImportException(
                    $"卡片數量不符（預期 {expectedCount}，實際 {dto.Cards.Count}）",
                    "INVALID_CARD_COUNT");
        }

        foreach (var card in dto.Cards)
        {
            if (TarotCards.GetById(card.CardId) is null)
                throw new ReadingImportException($"卡片 {card.CardId} 不存在", "INVALID_CARD");
            if (card.Orientation is not ("upright" or "reversed"))
                throw new ReadingImportException($"無效的牌位方向：{card.Orientation}", "INVALID_ORIENTATION");
        }

        // Reject duplicate position_index within the same payload
        if (dto.Cards.Select(c => c.PositionIndex).Distinct().Count() != dto.Cards.Count)
            throw new ReadingImportException("牌位重複", "INVALID_POSITION");

        // Reject duplicate cards within the same payload
        if (dto.Cards.Select(c => c.CardId).Distinct().Count() != dto.Cards.Count)
            throw new ReadingImportException("卡片重複", "INVALID_CARDS_DUPLICATE");

        var cardsPayload = dto.Cards.Select(c => new
        {
            card_id = c.CardId,
            orientation = c.Orientation,
            position_index = c.PositionIndex
        });

        var reading = new Reading
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SpreadType = SpreadTypeToString(spreadType),
            Question = dto.Question,
            Cards = JsonDocument.Parse(JsonSerializer.Serialize(cardsPayload)),
            CreatedAt = dto.DrawnAt ?? DateTime.UtcNow,
            ClientToken = dto.ClientToken
        };

        db.Readings.Add(reading);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Race: another concurrent request claimed the same client_token
            var winner = await db.Readings
                .FirstOrDefaultAsync(r => r.ClientToken == dto.ClientToken && r.UserId == userId);
            if (winner is not null)
                return ToResponseDto(winner, ResolveCards(winner));
            throw;
        }

        return ToResponseDto(reading, ResolveCards(reading));
    }

    public async Task<(List<ReadingResponseDto> Items, int TotalCount)> GetReadings(Guid userId, int page, int pageSize)
    {
        var query = db.Readings
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        var readings = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = readings.Select(r => ToResponseDto(r, ResolveCards(r))).ToList();
        return (items, totalCount);
    }

    public async Task<ReadingResponseDto?> GetReadingById(Guid userId, Guid readingId)
    {
        var reading = await db.Readings
            .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

        return reading is null ? null : ToResponseDto(reading, ResolveCards(reading));
    }

    public async Task<Reading?> GetRawReadingById(Guid userId, Guid readingId)
    {
        return await db.Readings
            .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);
    }

    public async Task<(List<Reading> Items, int TotalCount, bool IsTruncated)> GetReadingsForExport(Guid userId, int hardLimit)
    {
        var query = db.Readings.Where(r => r.UserId == userId);
        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(hardLimit)
            .ToListAsync();
        return (items, totalCount, totalCount > hardLimit);
    }

    public async Task<bool> DeleteReading(Guid userId, Guid readingId)
    {
        var reading = await db.Readings
            .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

        if (reading is null) return false;

        db.Readings.Remove(reading);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<ReadingStatsDto> GetStats(Guid userId)
    {
        var userReadings = db.Readings.Where(r => r.UserId == userId);

        var totalCount = await userReadings.CountAsync();
        var lastReadingAt = totalCount > 0
            ? await userReadings.MaxAsync(r => (DateTime?)r.CreatedAt)
            : null;

        // Spread usage via LINQ GroupBy
        var spreadUsage = await userReadings
            .GroupBy(r => r.SpreadType)
            .Select(g => new SpreadStatDto { SpreadType = g.Key, Count = g.Count() })
            .ToListAsync();

        // Top cards: use raw SQL to unnest JSONB array and aggregate card_id counts
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

        var topCardDtos = topCards.Select(tc =>
        {
            var cardInfo = TarotCards.GetById(tc.CardId);
            return new CardStatDto
            {
                CardId = tc.CardId,
                NameCht = cardInfo?.NameCht ?? tc.CardId,
                Count = tc.Count
            };
        }).ToList();

        return new ReadingStatsDto
        {
            TotalCount = totalCount,
            TopCards = topCardDtos,
            SpreadUsage = spreadUsage,
            LastReadingAt = lastReadingAt
        };
    }

    public async Task<ReadingResponseDto?> GetWeeklyFortune(Guid userId)
    {
        var weekStart = GetCurrentWeekStart();
        var reading = await db.Readings
            .Where(r => r.UserId == userId
                        && r.SpreadType == "weekly-fortune"
                        && r.CreatedAt >= weekStart)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        return reading is null ? null : ToResponseDto(reading, ResolveCards(reading));
    }

    public async Task<ReadingResponseDto> CreateWeeklyFortune(Guid userId)
    {
        var weekStart = GetCurrentWeekStart();
        var exists = await db.Readings
            .AnyAsync(r => r.UserId == userId
                           && r.SpreadType == "weekly-fortune"
                           && r.CreatedAt >= weekStart);

        if (exists)
            throw new InvalidOperationException("本週已經抽過週運了，請下週再來");

        return await CreateReading(userId, SpreadType.WeeklyFortune, "本週週運");
    }

    private static DateTime GetCurrentWeekStart()
    {
        var now = DateTime.UtcNow;
        var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
        return now.AddDays(-diff).Date;
    }

    // --- Helpers ---

    // Raw SQL result type for card stats query
    private class CardStatRaw
    {
        public string CardId { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private static string SpreadTypeToString(SpreadType type) => type switch
    {
        SpreadType.Single => "single",
        SpreadType.ThreeCardTime => "three-card-time",
        SpreadType.ThreeCardProblem => "three-card-problem",
        SpreadType.ThreeCardLinear => "three-card-linear",
        SpreadType.CelticCross => "celtic-cross",
        SpreadType.WeeklyFortune => "weekly-fortune",
        SpreadType.Custom => "custom",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    // Parses the spread-type string the client sends back during import.
    // weekly-fortune intentionally excluded — it has its own once-per-week flow.
    private static SpreadType ParseImportableSpread(string s) => s switch
    {
        "single" => SpreadType.Single,
        "three-card-time" => SpreadType.ThreeCardTime,
        "three-card-problem" => SpreadType.ThreeCardProblem,
        "three-card-linear" => SpreadType.ThreeCardLinear,
        "celtic-cross" => SpreadType.CelticCross,
        "custom" => SpreadType.Custom,
        _ => throw new ReadingImportException($"無效的牌陣類型：{s}", "INVALID_SPREAD")
    };

    private static List<TarotService.DrawnCardResult> ResolveCards(Reading reading)
    {
        var elements = reading.Cards.RootElement.EnumerateArray().ToList();

        // Custom spreads have no preset positions — numbered slots sized to the
        // stored card count. Fixed spreads resolve positions from their config.
        SpreadPosition[] positions;
        if (reading.SpreadType == "custom")
        {
            positions = TarotService.BuildCustomPositions(elements.Count);
        }
        else
        {
            var spreadType = reading.SpreadType switch
            {
                "single" => SpreadType.Single,
                "three-card" => SpreadType.ThreeCardTime,
                "three-card-time" => SpreadType.ThreeCardTime,
                "three-card-problem" => SpreadType.ThreeCardProblem,
                "three-card-linear" => SpreadType.ThreeCardLinear,
                "celtic-cross" => SpreadType.CelticCross,
                "weekly-fortune" => SpreadType.WeeklyFortune,
                _ => SpreadType.Single
            };
            positions = TarotService.GetPositions(spreadType);
        }

        var results = new List<TarotService.DrawnCardResult>();
        foreach (var element in elements)
        {
            var cardId = element.GetProperty("card_id").GetString()!;
            var orientation = element.GetProperty("orientation").GetString()!;
            var positionIndex = element.GetProperty("position_index").GetInt32();

            var card = TarotCards.GetById(cardId);
            if (card is null) continue;

            var position = positionIndex < positions.Length
                ? positions[positionIndex]
                : new SpreadPosition(positionIndex, "你的感受", "你對此問題最真實的內心感受");

            results.Add(new TarotService.DrawnCardResult(card, orientation, position));
        }

        return results;
    }

    private static ReadingResponseDto ToResponseDto(Reading reading, List<TarotService.DrawnCardResult> drawnCards)
    {
        return new ReadingResponseDto
        {
            Id = reading.Id,
            SpreadType = reading.SpreadType,
            Question = reading.Question,
            Interpretation = reading.Interpretation,
            Notes = reading.Notes,
            CreatedAt = reading.CreatedAt,
            Cards = drawnCards.Select(MapDrawnCardToDto).ToList()
        };
    }

    private static DrawnCardDto MapDrawnCardToDto(TarotService.DrawnCardResult dc) => new()
    {
        CardId = dc.Card.Id,
        Name = dc.Card.Name,
        NameCht = dc.Card.NameCht,
        Arcana = dc.Card.Arcana,
        Suit = dc.Card.Suit,
        Orientation = dc.Orientation,
        Meaning = dc.Orientation == "upright" ? dc.Card.MeaningUpright : dc.Card.MeaningReversed,
        Keywords = dc.Card.Keywords,
        PositionIndex = dc.Position.Index,
        PositionLabel = dc.Position.Label,
        PositionDescription = dc.Position.Description
    };
}
