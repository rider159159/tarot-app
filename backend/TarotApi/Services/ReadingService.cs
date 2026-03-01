using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TarotApi.Data;
using TarotApi.Models;
using TarotApi.Models.Dtos;

namespace TarotApi.Services;

public class ReadingService(TarotDbContext db, TarotService tarotService)
{
    public async Task<ReadingResponseDto> CreateReading(Guid userId, SpreadType spreadType, string? question)
    {
        var drawnCards = tarotService.DrawCards(spreadType);

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

    public async Task<bool> DeleteReading(Guid userId, Guid readingId)
    {
        var reading = await db.Readings
            .FirstOrDefaultAsync(r => r.Id == readingId && r.UserId == userId);

        if (reading is null) return false;

        db.Readings.Remove(reading);
        await db.SaveChangesAsync();
        return true;
    }

    // --- Helpers ---

    private static string SpreadTypeToString(SpreadType type) => type switch
    {
        SpreadType.Single => "single",
        SpreadType.ThreeCard => "three-card",
        SpreadType.CelticCross => "celtic-cross",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private static List<TarotService.DrawnCardResult> ResolveCards(Reading reading)
    {
        var cardsJson = reading.Cards.RootElement;
        var results = new List<TarotService.DrawnCardResult>();

        foreach (var element in cardsJson.EnumerateArray())
        {
            var cardId = element.GetProperty("card_id").GetString()!;
            var orientation = element.GetProperty("orientation").GetString()!;
            var positionIndex = element.GetProperty("position_index").GetInt32();

            var card = TarotCards.GetById(cardId);
            if (card is null) continue;

            var spreadType = reading.SpreadType switch
            {
                "single" => SpreadType.Single,
                "three-card" => SpreadType.ThreeCard,
                "celtic-cross" => SpreadType.CelticCross,
                _ => SpreadType.Single
            };

            var positions = TarotService.GetPositions(spreadType);
            var position = positionIndex < positions.Length
                ? positions[positionIndex]
                : new SpreadPosition(positionIndex, "", "");

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
            Cards = drawnCards.Select(dc => new DrawnCardDto
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
            }).ToList()
        };
    }
}
