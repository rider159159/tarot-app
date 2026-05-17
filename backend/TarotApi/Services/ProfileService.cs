using Microsoft.EntityFrameworkCore;
using TarotApi.Data;
using TarotApi.Models.Dtos;

namespace TarotApi.Services;

public class ProfileService(TarotDbContext db)
{
    // Returns the user's profile, self-healing if the row is missing.
    // The DB trigger handle_new_user() creates profiles for accounts created
    // after migration 002, but pre-002 accounts (and any case where the
    // trigger failed) have no row. Rather than 404, we backfill it here using
    // the email from the JWT, mirroring the trigger's display_name rule.
    public async Task<ProfileDto> GetProfile(Guid userId, string? email)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null)
        {
            profile = new Models.Profile
            {
                Id = userId,
                DisplayName = DeriveDisplayName(email),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Profiles.Add(profile);
            await db.SaveChangesAsync();
        }

        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            CreatedAt = profile.CreatedAt
        };
    }

    // Mirrors handle_new_user(): the local-part of the email.
    private static string DeriveDisplayName(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "使用者";
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }

    public async Task<ProfileDto?> UpdateProfile(Guid userId, string displayName)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null) return null;

        profile.DisplayName = displayName;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            CreatedAt = profile.CreatedAt
        };
    }
}
