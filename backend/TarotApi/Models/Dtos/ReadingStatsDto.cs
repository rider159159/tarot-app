namespace TarotApi.Models.Dtos;

public class ReadingStatsDto
{
    public int TotalCount { get; set; }
    public List<CardStatDto> TopCards { get; set; } = [];
    public List<SpreadStatDto> SpreadUsage { get; set; } = [];
    public DateTime? LastReadingAt { get; set; }
}

public class CardStatDto
{
    public string CardId { get; set; } = string.Empty;
    public string NameCht { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class SpreadStatDto
{
    public string SpreadType { get; set; } = string.Empty;
    public int Count { get; set; }
}
