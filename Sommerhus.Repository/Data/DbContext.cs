using Microsoft.EntityFrameworkCore;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;

namespace Sommerhus.Repository;

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

    public DbSet<HouseGroup> HouseGroups => Set <HouseGroup>();


    // Pricing
    public DbSet<PricePlan> PricePlans => Set<PricePlan>();
    public DbSet<PriceModifier> PriceModifiers => Set<PriceModifier>();
    public DbSet<SeasonPrice> SeasonPrices => Set<SeasonPrice>();
    public DbSet<SeasonCode> SeasonCodes => Set<SeasonCode>();
    public DbSet<SeasonSpan> SeasonSpans => Set<SeasonSpan>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Feature
        b.Entity<Feature>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Key).IsRequired().HasMaxLength(60);
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
            e.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(e => e.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
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
        b.Entity<PricePlan>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(4);
            e.HasOne<VacationHouse>()
                .WithMany()
                .HasForeignKey(x => x.HouseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.SeasonPrices)
                .WithOne(s => s.PricePlan!)
                .HasForeignKey(s => s.PricePlanId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        b.Entity<SeasonPrice>(e =>
        {
            e.Property(x => x.Code).IsRequired().HasMaxLength(10);
            e.HasIndex(x => new { x.PricePlanId, x.Code }).IsUnique();
            e.HasOne<PricePlan>()
                .WithMany(p => p.SeasonPrices)
                .HasForeignKey(x => x.PricePlanId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<SeasonCode>()
                .WithMany()
                .HasForeignKey(x => x.Code)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SeasonSpan>(e =>
        {
            e.HasOne<SeasonCode>()
                .WithMany()
                .HasForeignKey(x => x.Code)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<HouseGroup>()
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.StartDate).IsRequired();
            e.Property(x => x.EndDate).IsRequired();
            e.HasIndex(x => new { x.GroupId, x.StartDate, x.EndDate }).IsUnique();
        });

        b.Entity<PriceModifier>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Value).HasColumnType("TEXT");
            e.HasIndex(x => x.RatePlanId);
        });
    }
}
