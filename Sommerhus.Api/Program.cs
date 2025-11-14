using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Sommerhus.Application.Admin.Areas;
using Sommerhus.Application.Admin.Cities;
using Sommerhus.Application.Admin.Features;
using Sommerhus.Application.Admin.Houses;
using Sommerhus.Application.Admin.Images;
using Sommerhus.Application.Admin.HouseGroups;
using Sommerhus.Application.Admin.Pricing;
using Sommerhus.Application.Storage;
using Sommerhus.Application.Public.Areas;
using Sommerhus.Application.Public.Cities;
using Sommerhus.Application.Public.Features;
using Sommerhus.Application.Public.Houses;
using Sommerhus.Application.Public.Images;
using Sommerhus.Application.Public.ZipCodes;
using Sommerhus.Application.Pricing.Abstractions;
using Sommerhus.Application.Pricing.Engine;
using Sommerhus.Application.Pricing.Engine.Rules;
using Sommerhus.Repository;
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
using Sommerhus.Api.Infrastructure;
using Sommerhus.Api.Infrastructure.Storage;
using Microsoft.IdentityModel.Tokens;
using Sommerhus.Api.Infrastructure.Auth;
using Sommerhus.Repository.Identity;
using Sommerhus.Contracts.Security;


namespace Sommerhus.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSommerhusPersistence(builder.Configuration);

        // Bind StorageOptions
        //builder.Services.Configure<StorageOptions>(
        //    builder.Configuration.GetSection(StorageOptions.SectionName));

        builder.Services.AddControllers();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminRoles.Admin, policy => policy.RequireRole(AdminRoles.Admin));
        });

        builder.Services.AddScoped<IAdminAreaService, AdminAreaService>();
        builder.Services.AddScoped<IAdminCityService, AdminCityService>();
        builder.Services.AddScoped<IAdminFeatureService, AdminFeatureService>();
        builder.Services.AddScoped<IAdminHouseGroupService, AdminHouseGroupService>();
        builder.Services.AddScoped<IAdminHouseService, AdminHouseService>();
        builder.Services.AddScoped<IAdminPricingService, AdminPricingService>();
        builder.Services.AddScoped<IAdminAreaImageService, AdminAreaImageService>();
        builder.Services.AddScoped<IAdminCityImageService, AdminCityImageService>();
        builder.Services.AddScoped<IAdminHouseImageService, AdminHouseImageService>();
        builder.Services.AddScoped<IHouseQueryService, HouseQueryService>();
        builder.Services.AddScoped<IAreaQueryService, AreaQueryService>();
        builder.Services.AddScoped<IFeatureQueryService, FeatureQueryService>();
        builder.Services.AddScoped<ICityQueryService, CityQueryService>();
        builder.Services.AddScoped<IZipCodeQueryService, ZipCodeQueryService>();
        builder.Services.AddScoped<IHouseImageQueryService, HouseImageQueryService>();
        builder.Services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<IImageStorage, PhysicalImageStorage>();

        builder.Services.AddScoped<IRatePlanStore, EfRatePlanStore>();
        builder.Services.AddScoped<IPriceRule, BaseNightlyRateRule>();
        builder.Services.AddScoped<IPriceRule, CleaningFeeRule>();
        builder.Services.AddScoped<IPricingPipeline, PricingPipeline>();

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sommerhus API", Version = "v1" });
            c.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
        });

        // CORS
        builder.Services.AddCors(opt =>
        {
            opt.AddPolicy("mvc", p => p
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins("https://localhost:5001", "https://localhost:7202") // eller dit domæne
                .AllowCredentials());
        });
        var app = builder.Build();


        app.UseStaticFiles();
        app.UseCors("mvc");

        app.UseMiddleware<ProblemDetailsMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        await app.RunAsync();
    }
}
