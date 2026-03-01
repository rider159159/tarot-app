using Microsoft.EntityFrameworkCore;
using TarotApi.Models;

namespace TarotApi.Data;

public class TarotDbContext : DbContext
{
    public TarotDbContext(DbContextOptions<TarotDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.ToTable("profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Reading>(entity =>
        {
            entity.ToTable("readings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.SpreadType).HasColumnName("spread_type");
            entity.Property(e => e.Question).HasColumnName("question");
            entity.Property(e => e.Cards).HasColumnName("cards").HasColumnType("jsonb");
            entity.Property(e => e.Interpretation).HasColumnName("interpretation");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
