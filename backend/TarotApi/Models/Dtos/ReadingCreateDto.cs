namespace TarotApi.Models.Dtos;

public class ReadingCreateDto
{
    public SpreadType SpreadType { get; set; }
    public string? Question { get; set; }

    // Number of cards to draw. Only used when SpreadType is Custom (valid range 1–10).
    public int? CardCount { get; set; }
}
