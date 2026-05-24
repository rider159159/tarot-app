using Microsoft.EntityFrameworkCore;
using TarotApi.Data;
using TarotApi.Models.Dtos;

namespace TarotApi.Services;

// Admin-only operations against Supabase's auth.users table. Email and
// email-verification status live there, not in public.profiles, so these
// queries reach into the auth schema directly via the existing connection.
public class AdminService(TarotDbContext db)
{
    public async Task<(List<AdminUserDto> Items, int TotalCount)> ListUsers(
        string? search, int page, int pageSize)
    {
        object patternParam = string.IsNullOrWhiteSpace(search)
            ? DBNull.Value
            : $"%{search.Trim()}%";
        var offset = (page - 1) * pageSize;

        var countRows = await db.Database
            .SqlQueryRaw<CountRow>(
                """
                SELECT COUNT(*) AS Total
                FROM auth.users u
                LEFT JOIN public.profiles p ON p.id = u.id
                WHERE u.deleted_at IS NULL
                  AND ({0}::text IS NULL
                       OR u.email ILIKE {0}::text
                       OR coalesce(p.display_name, '') ILIKE {0}::text)
                """, patternParam)
            .ToListAsync();
        var totalCount = (int)countRows[0].Total;

        var items = await db.Database
            .SqlQueryRaw<AdminUserDto>(
                """
                SELECT u.id,
                       p.display_name AS DisplayName,
                       u.email,
                       (u.email_confirmed_at IS NOT NULL) AS EmailVerified,
                       u.created_at AS CreatedAt
                FROM auth.users u
                LEFT JOIN public.profiles p ON p.id = u.id
                WHERE u.deleted_at IS NULL
                  AND ({0}::text IS NULL
                       OR u.email ILIKE {0}::text
                       OR coalesce(p.display_name, '') ILIKE {0}::text)
                ORDER BY u.created_at DESC
                LIMIT {1} OFFSET {2}
                """, patternParam, pageSize, offset)
            .ToListAsync();

        return (items, totalCount);
    }

    // Deletes the auth.users row. public.profiles and public.readings both
    // declare ON DELETE CASCADE on auth.users(id), so associated rows go too.
    public async Task<bool> DeleteUser(Guid userId)
    {
        var affected = await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM auth.users WHERE id = {0} AND deleted_at IS NULL", userId);
        return affected > 0;
    }

    private class CountRow
    {
        public long Total { get; set; }
    }
}
