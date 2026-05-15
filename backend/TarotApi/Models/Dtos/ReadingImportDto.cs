namespace TarotApi.Models.Dtos;

// Payload to import a previously-drawn anonymous reading after login.
// Cards are trusted as-is — no abuse incentive (users won't fake their own
// readings). client_token is the idempotency key.
public class ReadingImportDto
{
    public Guid ClientToken { get; set; }
    public string SpreadType { get; set; } = string.Empty;
    public string? Question { get; set; }
    public List<ImportedCardDto> Cards { get; set; } = [];
    public DateTime? DrawnAt { get; set; }
}

public class ImportedCardDto
{
    public string CardId { get; set; } = string.Empty;
    public string Orientation { get; set; } = string.Empty; // "upright" | "reversed"
    public int PositionIndex { get; set; }
}
