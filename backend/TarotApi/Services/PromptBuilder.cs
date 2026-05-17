using TarotApi.Data;
using TarotApi.Models;
using TarotApi.Models.Dtos;

namespace TarotApi.Services;

public class PromptBuilder
{
    private const string FormatVersion = "1.0";
    private const string Source = "rtarot.zeabur.app";
    private const string AiPromptSuggestion =
        "你是一位資深塔羅諮商者。請以這次抽牌結果為基礎，針對使用者的問題提供深入解讀，" +
        "包含每張牌在其位置的意義、牌與牌之間的關聯，以及給予的建議。" +
        "請用繁體中文，溫暖但不過度肯定的語氣。";

    private static readonly Dictionary<string, string> SpreadTypeLabels = new()
    {
        ["single"] = "單張牌",
        ["three-card-time"] = "三張牌 — 時間流（過去/現在/未來）",
        ["three-card-problem"] = "三張牌 — 問題對策",
        ["three-card-linear"] = "三張牌 — 線性展開",
        ["celtic-cross"] = "凱爾特十字",
        ["weekly-fortune"] = "每週週運",
        ["custom"] = "自定義牌陣"
    };

    public ExportPayloadDto BuildSingleExport(Reading reading) => new()
    {
        Metadata = BuildMetadata(),
        Reading = BuildReadingDto(reading),
        AiPromptSuggestion = AiPromptSuggestion
    };

    public ExportBatchDto BuildBatchExport(List<Reading> readings, int totalCount, bool isTruncated) => new()
    {
        Metadata = BuildMetadata(),
        TotalCount = totalCount,
        ExportedCount = readings.Count,
        IsTruncated = isTruncated,
        Readings = readings.Select(BuildReadingDto).ToList()
    };

    public ExportReadingDto BuildReadingDto(Reading reading)
    {
        var cardElements = reading.Cards.RootElement.EnumerateArray().ToList();

        // Custom spreads have no preset positions — numbered slots sized to the card count.
        var positions = reading.SpreadType == "custom"
            ? TarotService.BuildCustomPositions(cardElements.Count)
            : TarotService.GetPositions(ParseSpreadType(reading.SpreadType));

        var cards = new List<ExportCardDto>();
        foreach (var el in cardElements)
        {
            var cardId = el.GetProperty("card_id").GetString()!;
            var orientation = el.GetProperty("orientation").GetString()!;
            var positionIndex = el.GetProperty("position_index").GetInt32();

            var info = TarotCards.GetById(cardId);
            if (info is null) continue;

            var positionLabel = positionIndex < positions.Length
                ? positions[positionIndex].Label
                : "你的感受";

            cards.Add(new ExportCardDto
            {
                Position = positionLabel,
                Name = info.NameCht,
                NameEn = info.Name,
                Orientation = orientation == "reversed" ? "逆位" : "正位",
                Keywords = info.Keywords.ToList(),
                Meaning = orientation == "reversed" ? info.MeaningReversed : info.MeaningUpright
            });
        }

        return new ExportReadingDto
        {
            Id = reading.Id,
            DrawnAt = reading.CreatedAt,
            SpreadType = SpreadTypeLabels.GetValueOrDefault(reading.SpreadType, reading.SpreadType),
            SpreadTypeKey = reading.SpreadType,
            Question = string.IsNullOrWhiteSpace(reading.Question) ? null : reading.Question,
            Cards = cards
        };
    }

    private static ExportMetadataDto BuildMetadata() => new()
    {
        ExportedAt = DateTime.UtcNow,
        Source = Source,
        FormatVersion = FormatVersion
    };

    private static SpreadType ParseSpreadType(string s) => s switch
    {
        "single" => SpreadType.Single,
        "three-card-time" => SpreadType.ThreeCardTime,
        "three-card-problem" => SpreadType.ThreeCardProblem,
        "three-card-linear" => SpreadType.ThreeCardLinear,
        "celtic-cross" => SpreadType.CelticCross,
        "weekly-fortune" => SpreadType.WeeklyFortune,
        _ => SpreadType.Single
    };
}
