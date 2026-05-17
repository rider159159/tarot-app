using System.Security.Claims;

namespace TarotApi.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return Guid.Parse(sub);
    }

    // Supabase puts the address in the "email" claim; .NET may also surface it
    // as ClaimTypes.Email depending on claim mapping. Returns null if absent.
    public static string? GetEmail(this ClaimsPrincipal user)
        => user.FindFirstValue("email") ?? user.FindFirstValue(ClaimTypes.Email);
}
