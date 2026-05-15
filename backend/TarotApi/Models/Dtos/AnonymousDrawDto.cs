using TarotApi.Models;

namespace TarotApi.Models.Dtos;

// Anonymous draw request. No clientToken here — the client owns that and
// only sends it later during import (after the user logs in).
public class AnonymousDrawDto
{
    public SpreadType SpreadType { get; set; }
    public string? Question { get; set; }
}
