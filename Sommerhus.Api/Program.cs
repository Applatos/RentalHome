using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Sommerhus.Api.Extensions;
using Sommerhus.Api.Infrastructure;
using Sommerhus.Api.Infrastructure.Auth;
using Sommerhus.Core.Identity;
using Sommerhus.Core;
using System.Globalization;

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

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        // Bearer validation reads JwtOptions when the options are first used, not while the
        // builder is still being configured. The key comes from the environment in production
        // (and from the test host in tests), and those sources are only complete by then.
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwt) =>
            {
                var jwtOptions = jwt.Value;
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
            options.AddPolicy(AppRoles.Admin, policy => policy.RequireRole(AppRoles.Admin));
        });


        builder.Services
            .AddAdminServices()
            .AddPublicServices()
            .AddOwnerServices()
            .AddStorageServices(builder.Configuration)
            .AddPricingServices()
            .AddDevServices();

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Sommerhus API", Version = "v1" });
            c.CustomSchemaIds(t => t.FullName!.Replace('+', '.'));
            
            // Add JWT Authentication
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        // CORS
        builder.Services.AddCors(opt =>
        {
            opt.AddPolicy("mvc", p => p
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithOrigins("https://localhost:5001", "https://localhost:7202") // or your domain
                .AllowCredentials());
        });
        var app = builder.Build();

        var supportedCultures = new[]
        {
            new CultureInfo("da-DK"),
            new CultureInfo("en-GB")
        };

        var localizationOptions = new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("da-DK"),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures
        };

        localizationOptions.RequestCultureProviders =
        [
            new QueryStringRequestCultureProvider(),
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider()
        ];


        app.UseStaticFiles();
        app.UseRequestLocalization(localizationOptions);
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
