using TarotApi.Models;

namespace TarotApi.Models.Dtos;

// Anonymous draw request. No clientToken here — the client owns that and
// only sends it later during import (after the user logs in).
public class AnonymousDrawDto
{
    public SpreadType SpreadType { get; set; }
    public string? Question { get; set; }

    // Number of cards to draw. Only used when SpreadType is Custom (valid range 1–10).
    public int? CardCount { get; set; }
}
