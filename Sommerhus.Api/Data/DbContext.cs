// Sommerhus.Api/Data/DbContext.cs
using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Models;

namespace Sommerhus.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VacationHouse> Houses => Set<VacationHouse>();
    public DbSet<HouseImage> Images => Set<HouseImage>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<AreaImage> AreaImages => Set<AreaImage>();
    public DbSet<HouseFeatureValue> HouseFeatureValues => Set<HouseFeatureValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // her sætter du relationen
        modelBuilder.Entity<Area>()
            .HasMany(a => a.AreaImages)
            .WithOne(ai => ai.Area)
            .HasForeignKey(ai => ai.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<VacationHouse>()
            .HasMany(h => h.Images)
            .WithOne(i => i.House)
            .HasForeignKey(i => i.HouseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<HouseImage>(b =>
        {
            b.Property(i => i.FileName).HasMaxLength(300).IsRequired();

            // (Valgfri) Unikt filnavn pr. hus
            b.HasIndex(i => new { i.HouseId, i.FileName }).IsUnique();

            // (Valgfri) Gem enum som string i DB for læsbarhed
            b.Property(i => i.Kind).HasConversion<string>();
        });

        base.OnModelCreating(modelBuilder);
    }
}
