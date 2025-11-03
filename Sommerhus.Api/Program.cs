using Microsoft.OpenApi.Models;
using Sommerhus.Repository;
using Sommerhus.Api.Infrastructure.Storage;
using Sommerhus.Api.Pricing;
using Sommerhus.Pricing.Abstractions;
using Sommerhus.Pricing.Engine;
using Sommerhus.Pricing.Engine.Rules;


namespace Sommerhus.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSommerhusPersistence(builder.Configuration);

        builder.Services.AddControllers();

        builder.Services.AddScoped<Services.Admin.Houses.AdminHouseService>();
        builder.Services.AddScoped<Services.Admin.Areas.AdminAreaService>();
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

        app.UseHttpsRedirection();
        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        await app.RunAsync();
    }
}
