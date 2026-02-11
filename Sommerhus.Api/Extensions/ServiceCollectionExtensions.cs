using Sommerhus.Core.Services.Admin.Areas;
using Sommerhus.Core.Services.Admin.Audit;
using Sommerhus.Core.Services.Admin.Cities;
using Sommerhus.Core.Services.Admin.Features;
using Sommerhus.Core.Services.Admin.Houses;
using Sommerhus.Core.Services.Admin.Images;
using Sommerhus.Core.Services.Admin.HouseGroups;
using Sommerhus.Core.Services.Admin.Calendars;
using Sommerhus.Core.Services.Admin.Availability;
using Sommerhus.Core.Services.Admin.Lifecycle;
using Sommerhus.Core.Data;
using Sommerhus.Core.Services.Admin.Pricing;
using Sommerhus.Core.Services.Public.Areas;
using Sommerhus.Core.Services.Public.Cities;
using Sommerhus.Core.Services.Public.Features;
using Sommerhus.Core.Services.Public.Houses;
using Sommerhus.Core.Services.Public.Images;
using Sommerhus.Core.Services.Public.Pricing;
using Sommerhus.Core.Services.Public.Availability;
using Sommerhus.Core.Services.Public.ZipCodes;
using Sommerhus.Core.Services.Pricing.Abstractions;
using Sommerhus.Core.Services.Pricing.Engine;
using Sommerhus.Core.Services.Pricing.Engine.Rules;
using Sommerhus.Core.Services.Storage;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Core.Services.Owner;
using Sommerhus.Core.Services.Pricing;

namespace Sommerhus.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdminServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminAreaService, AdminAreaService>();
        services.AddScoped<IAdminCityService, AdminCityService>();
        services.AddScoped<IAdminFeatureService, AdminFeatureService>();
        services.AddScoped<IAdminHouseGroupService, AdminHouseGroupService>();
        services.AddScoped<IAdminHouseService, AdminHouseService>();
        services.AddScoped<IAdminHouseFeatureService, AdminHouseFeatureService>();
        services.AddScoped<IAdminHousePricingService, AdminHousePricingService>();
        services.AddScoped<IAdminPricingService, AdminPricingService>();
        services.AddScoped<IAdminAreaImageService, AdminAreaImageService>();
        services.AddScoped<IAdminCityImageService, AdminCityImageService>();
        services.AddScoped<IAdminHouseImageService, AdminHouseImageService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEntityLifecycleService, EntityLifecycleService>();
        services.AddScoped<IAdminCalendarService, AdminCalendarService>();
        services.AddScoped<IAdminAvailabilityService, AdminAvailabilityService>();
        services.AddScoped<IStressDataGenerator, StressDataGenerator>();

        return services;
    }

    public static IServiceCollection AddPublicServices(this IServiceCollection services)
    {
        services.AddScoped<IHouseQueryService, HouseQueryService>();
        services.AddScoped<IAreaQueryService, AreaQueryService>();
        services.AddScoped<IFeatureQueryService, FeatureQueryService>();
        services.AddScoped<ICityQueryService, CityQueryService>();
        services.AddScoped<IZipCodeQueryService, ZipCodeQueryService>();
        services.AddScoped<IHouseImageQueryService, HouseImageQueryService>();
        services.AddScoped<IPricingQuoteService, AdminPricingService>();
        services.AddScoped<IAvailabilityQueryService, AvailabilityQueryService>();

        return services;
    }

    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IImageStorage, PhysicalImageStorage>();

        return services;
    }

    public static IServiceCollection AddOwnerServices(this IServiceCollection services)
    {
        services.AddScoped<IOwnerAuthorizationService, OwnerAuthorizationService>();
        services.AddScoped<IOwnerHouseService, OwnerHouseService>();

        return services;
    }

    public static IServiceCollection AddPricingServices(this IServiceCollection services)
    {
        services.AddScoped<IRatePlanStore, EfRatePlanStore>();
        services.AddScoped<IPriceRule, BaseNightlyRateRule>();
        services.AddScoped<IPriceRule, GuestFeeRule>();
        services.AddScoped<IPriceRule, CleaningFeeRule>();
        services.AddScoped<IPricingPipeline, PricingPipeline>();

        return services;
    }
}
