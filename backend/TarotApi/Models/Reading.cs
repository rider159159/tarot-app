using System.Text.Json;

namespace TarotApi.Models;

public class Reading
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SpreadType { get; set; } = string.Empty;
    public string? Question { get; set; }
    public JsonDocument Cards { get; set; } = null!;
    public string? Interpretation { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    // Client-generated UUID. NULL for readings created directly while logged in;
    // populated when an anonymous draw is later imported. Partial unique index
    // makes the import endpoint idempotent.
    public Guid? ClientToken { get; set; }
}
