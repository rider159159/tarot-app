namespace TarotApi.Models.Dtos;

// Same shape as ReadingResponseDto minus Id/Interpretation/Notes (which only
// exist for persisted readings). DrawnAt replaces CreatedAt to signal the
// distinction: nothing is in the DB yet.
public class AnonymousDrawResponseDto
{
    public string SpreadType { get; set; } = string.Empty;
    public string? Question { get; set; }
    public DateTime DrawnAt { get; set; }
    public List<DrawnCardDto> Cards { get; set; } = [];
}
