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
    public DbSet<ZipCode> ZipCodes => Set<ZipCode>();

    // Ny:
    public DbSet<CityImage> CityImages => Set<CityImage>();

    public DbSet<HouseFeatureValue> HouseFeatureValues => Set<HouseFeatureValue>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // … (eksisterende mappings for Houses/Images/Features/ZipCodes)

        // City
        b.Entity<City>(e =>
        {
            e.ToTable("City");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(80).IsRequired();
            e.Property(x => x.Zip).HasMaxLength(10).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(80);
            e.Property(x => x.Text); // kan være stor, behold som nvarchar(max)
            e.HasIndex(x => x.Zip);
            e.HasIndex(x => x.Name);
            e.HasIndex(x => x.Slug);
        });

        // CityImage
        b.Entity<CityImage>(e =>
        {
            e.ToTable("CityImage");
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).HasMaxLength(200).IsRequired();
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne<City>(x => x.City!)
                .WithMany(c => c.Images)
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.CityId, x.SortOrder });
        });

        // ZipCode (uændret)
        b.Entity<ZipCode>(e =>
        {
            e.ToTable("ZipCode");
            e.HasKey(x => x.Id);
            e.Property(x => x.Zip).HasMaxLength(10).IsRequired();
            e.Property(x => x.City).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Zip);
            e.HasIndex(x => x.City);
        });

        base.OnModelCreating(b);
    }
}
