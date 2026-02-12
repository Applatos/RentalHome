using Microsoft.EntityFrameworkCore;
using Sommerhus.Domain.Models;
using Sommerhus.Domain.Models.Pricing;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Sommerhus.Core.Identity;

namespace Sommerhus.Core;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
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
    public DbSet<SeasonCalendar> SeasonCalendars => Set<SeasonCalendar>();

    // Availability
    public DbSet<AvailabilityBlock> AvailabilityBlocks => Set<AvailabilityBlock>();

    // Bookings
    public DbSet<Booking> Bookings => Set<Booking>();

    // Audit
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Feature
        b.Entity<Feature>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Key).IsRequired().HasMaxLength(60);
            e.HasIndex(x => x.Key).IsUnique();
            e.Property(x => x.Options).HasMaxLength(500);
            e.HasIndex(x => x.IsSearchable);
        });

        // House
        b.Entity<VacationHouse>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.SearchKeywords).HasMaxLength(500);
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
            e.Property(x => x.Status).HasDefaultValue(Sommerhus.Domain.Models.EntityStatus.Draft);
            e.HasIndex(x => x.Status);
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
            e.HasOne(x => x.CalendarOverride)
                .WithMany()
                .HasForeignKey(x => x.CalendarOverrideId)
                .OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.OwnerId).HasMaxLength(450);
            e.HasIndex(x => x.OwnerId);
        });

        b.Entity<HouseImage>(e =>
        {
            e.Property(x => x.FileName).IsRequired().HasMaxLength(300);
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

        b.Entity<SeasonCode>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(10);
        });

        // Area
        b.Entity<Area>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Status).HasDefaultValue(Sommerhus.Domain.Models.EntityStatus.Draft);
            e.HasIndex(x => x.Status);

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
            e.HasOne<SeasonCode>()
                .WithMany()
                .HasForeignKey(x => x.Code)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<SeasonCalendar>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            e.HasMany(x => x.Spans)
                .WithOne(s => s.Calendar)
                .HasForeignKey(s => s.CalendarId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<HouseGroup>(e =>
        {
            e.HasOne(x => x.DefaultCalendar)
                .WithMany()
                .HasForeignKey(x => x.DefaultCalendarId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<SeasonSpan>(e =>
        {
            e.HasOne<SeasonCode>()
                .WithMany()
                .HasForeignKey(x => x.Code)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.StartDate).IsRequired();
            e.Property(x => x.EndDate).IsRequired();
            e.HasIndex(x => new { x.CalendarId, x.StartDate, x.EndDate }).IsUnique();
        });

        // Availability
        b.Entity<AvailabilityBlock>(e =>
        {
            e.HasOne(x => x.House)
                .WithMany(h => h.AvailabilityBlocks)
                .HasForeignKey(x => x.HouseId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.StartDate).IsRequired();
            e.Property(x => x.EndDate).IsRequired();
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.HouseId, x.StartDate, x.EndDate });
        });

        // Booking
        b.Entity<Booking>(e =>
        {
            e.HasOne(x => x.House)
                .WithMany()
                .HasForeignKey(x => x.HouseId)
                .OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.HouseId);
            e.HasIndex(x => x.Status);
            e.Property(x => x.Currency).IsRequired().HasMaxLength(10);
            e.Property(x => x.TotalPrice).HasPrecision(18, 2);
            e.Property(x => x.GuestNote).HasMaxLength(500);
            e.Property(x => x.OwnerNote).HasMaxLength(500);
            e.Property(x => x.CancelledBy).HasMaxLength(256);
            e.HasOne(x => x.AvailabilityBlock)
                .WithMany()
                .HasForeignKey(x => x.AvailabilityBlockId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Audit entry
        b.Entity<AuditEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedOnAdd();
            e.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
            e.Property(x => x.EntityId).IsRequired().HasMaxLength(64);
            e.Property(x => x.ChangedBy).IsRequired().HasMaxLength(256);
            e.HasIndex(x => new { x.EntityType, x.EntityId });
            e.HasIndex(x => x.ChangedAtUtc);
        });

        b.Entity<PriceModifier>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.RatePlanId);
        });
    }
}
