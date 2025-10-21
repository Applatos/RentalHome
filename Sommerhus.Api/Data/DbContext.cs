using Microsoft.EntityFrameworkCore;
using Sommerhus.Api.Models;
using System.Reflection.Emit;

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
    }
}
