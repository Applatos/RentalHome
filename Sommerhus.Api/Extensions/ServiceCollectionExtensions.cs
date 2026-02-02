using Sommerhus.Application.Admin.Areas;
using Sommerhus.Application.Admin.Cities;
using Sommerhus.Application.Admin.Features;
using Sommerhus.Application.Admin.Houses;
using Sommerhus.Application.Admin.Images;
using Sommerhus.Application.Admin.HouseGroups;
using Sommerhus.Application.Admin.Pricing;
using Sommerhus.Application.Public.Areas;
using Sommerhus.Application.Public.Cities;
using Sommerhus.Application.Public.Features;
using Sommerhus.Application.Public.Houses;
using Sommerhus.Application.Public.Images;
using Sommerhus.Application.Public.ZipCodes;
using Sommerhus.Application.Pricing.Abstractions;
using Sommerhus.Application.Pricing.Engine;
using Sommerhus.Application.Pricing.Engine.Rules;
using Sommerhus.Application.Storage;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Repository.Admin.Areas;
using Sommerhus.Repository.Admin.Cities;
using Sommerhus.Repository.Admin.Features;
using Sommerhus.Repository.Admin.Houses;
using Sommerhus.Repository.Admin.Images;
using Sommerhus.Repository.Admin.HouseGroups;
using Sommerhus.Repository.Admin.Pricing;
using Sommerhus.Repository.Pricing;
using Sommerhus.Repository.Public.Areas;
using Sommerhus.Repository.Public.Cities;
using Sommerhus.Repository.Public.Features;
using Sommerhus.Repository.Public.Houses;
using Sommerhus.Repository.Public.Images;
using Sommerhus.Repository.Public.ZipCodes;

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
        services.AddScoped<IAdminPricingService, AdminPricingService>();
        services.AddScoped<IAdminAreaImageService, AdminAreaImageService>();
        services.AddScoped<IAdminCityImageService, AdminCityImageService>();
        services.AddScoped<IAdminHouseImageService, AdminHouseImageService>();

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

    public static IServiceCollection AddPricingServices(this IServiceCollection services)
    {
        services.AddScoped<IRatePlanStore, EfRatePlanStore>();
        services.AddScoped<IPriceRule, BaseNightlyRateRule>();
        services.AddScoped<IPriceRule, CleaningFeeRule>();
        services.AddScoped<IPricingPipeline, PricingPipeline>();

        return services;
    }
}
