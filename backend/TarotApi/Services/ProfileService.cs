using Microsoft.EntityFrameworkCore;
using TarotApi.Data;
using TarotApi.Models.Dtos;

namespace TarotApi.Services;

public class ProfileService(TarotDbContext db)
{
    public async Task<ProfileDto?> GetProfile(Guid userId)
    {
        var profile = await db.Profiles.FirstOrDefaultAsync(p => p.Id == userId);
        if (profile is null) return null;

        return new ProfileDto
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            CreatedAt = profile.CreatedAt
        };
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
