namespace TarotApi.Models.Dtos;

public class ReadingResponseDto
{
    public Guid Id { get; set; }
    public string SpreadType { get; set; } = string.Empty;
    public string? Question { get; set; }
    public List<DrawnCardDto> Cards { get; set; } = [];
    public string? Interpretation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DrawnCardDto
{
    public string CardId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameCht { get; set; } = string.Empty;
    public string Arcana { get; set; } = string.Empty;
    public string? Suit { get; set; }
    public string Orientation { get; set; } = string.Empty; // "upright" | "reversed"
    public string Meaning { get; set; } = string.Empty; // resolved meaning based on orientation
    public string[] Keywords { get; set; } = [];
    public int PositionIndex { get; set; }
    public string PositionLabel { get; set; } = string.Empty;
    public string PositionDescription { get; set; } = string.Empty;
}
