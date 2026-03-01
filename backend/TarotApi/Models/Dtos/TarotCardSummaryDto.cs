namespace TarotApi.Models.Dtos;

public class TarotCardSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameCht { get; set; } = string.Empty;
    public string Arcana { get; set; } = string.Empty;
    public string? Suit { get; set; }
    public int Number { get; set; }
}
