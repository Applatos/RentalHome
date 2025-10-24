using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Models;
using Sommerhus.Pricing.Models;

namespace Sommerhus.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<VacationHouse> Houses => Set<VacationHouse>();
    public DbSet<HouseImage> Images => Set<HouseImage>();
    public DbSet<HouseFeatureValue> HouseFeatures => Set<HouseFeatureValue>();
    public DbSet<Feature> Features => Set<Feature>();

    public DbSet<City> Cities => Set<City>();
    public DbSet<CityImage> CityImages => Set<CityImage>();

    public DbSet<Area> Areas => Set<Area>();
    public DbSet<AreaImage> AreaImages => Set<AreaImage>();

    public DbSet<RatePlan> RatePlans => Set<RatePlan>();
    public DbSet<RateSeason> RateSeasons => Set<RateSeason>();
    public DbSet<RateModifier> RateModifiers => Set<RateModifier>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Feature
        b.Entity<Feature>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Key).IsRequired().HasMaxLength(60).UseCollation("NOCASE");
            e.HasIndex(x => x.Key).IsUnique();
        });

        // House
        b.Entity<VacationHouse>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.HasOne(x => x.City)
                .WithMany(c => c.Houses)
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Areas)
                .WithMany(a => a.Houses)
                .UsingEntity<Dictionary<string, object>>(
                    "HouseAreas",
                    j => j.HasOne<Area>()
                        .WithMany()
                        .HasForeignKey("AreaId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<VacationHouse>()
                        .WithMany()
                        .HasForeignKey("HouseId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("HouseId", "AreaId");
                        j.ToTable("HouseAreas");
                    });
            e.HasMany(x => x.Images)
                .WithOne(i => i.House!)
                .HasForeignKey(i => i.HouseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.HouseFeatures)
                .WithOne(v => v.House!)
                .HasForeignKey(v => v.HouseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<HouseImage>(e =>
        {
            e.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        });

        b.Entity<HouseFeatureValue>(e =>
        {
            e.HasOne(v => v.Feature)
                .WithMany()
                .HasForeignKey(v => v.FeatureId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(v => v.RawValue).HasMaxLength(200);
        });

        // City
        b.Entity<City>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);

            e.HasMany(x => x.Images)
                .WithOne(i => i.City!)
                .HasForeignKey(i => i.CityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Area
        b.Entity<Area>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);

            e.HasMany(x => x.AreaImages)
                .WithOne(i => i.Area!)
                .HasForeignKey(i => i.AreaId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Cities)
                .WithMany(c => c.Areas)
                .UsingEntity<Dictionary<string, object>>(
                    "AreaCities",
                    j => j.HasOne<City>()
                        .WithMany()
                        .HasForeignKey("CityId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<Area>()
                        .WithMany()
                        .HasForeignKey("AreaId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("AreaId", "CityId");
                        j.ToTable("AreaCities");
                    });
            });

        // Rate plan + pricing
        b.Entity<RatePlan>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            e.HasOne<VacationHouse>()
                .WithMany()
                .HasForeignKey(x => x.HouseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Seasons)
                .WithOne(s => s.RatePlan!)
                .HasForeignKey(s => s.RatePlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Modifiers)
                .WithOne(m => m.RatePlan!)
                .HasForeignKey(m => m.RatePlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RateSeason>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.NightlyPrice).HasColumnType("TEXT");
            e.HasIndex(x => new { x.RatePlanId, x.StartDate, x.EndDate });
        });

        b.Entity<RateModifier>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Value).HasColumnType("TEXT");
            e.HasIndex(x => x.RatePlanId);
        });
    }
}
