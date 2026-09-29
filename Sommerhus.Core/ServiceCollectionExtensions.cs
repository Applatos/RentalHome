using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sommerhus.Core.Data;
using Sommerhus.Core.Identity;

namespace Sommerhus.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSommerhusPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Environment variables are expanded so a checked-in connection string can name a
        // machine-independent location (e.g. "%LOCALAPPDATA%\Sommerhus\sommerhus.db") without
        // committing an absolute path containing a user name. A string with no variables is
        // returned unchanged.
        var connectionString = Environment.ExpandEnvironmentVariables(
            configuration.GetConnectionString("Default") ?? "Data Source=sommerhus.db");
        var provider = ResolveProvider(configuration["DatabaseProvider"], connectionString);

        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            switch (provider)
            {
                case DatabaseProvider.SqlServer:
                    options.UseSqlServer(connectionString);
                    break;
                case DatabaseProvider.Sqlite:
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
            }

            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddOptions<DefaultAdminOptions>()
            .BindConfiguration(DefaultAdminOptions.SectionName)
            .ValidateOnStart();
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<AdminIdentitySeeder>();

        services.AddHostedService<MigrationHostedService>();

        return services;
    }

    private static string ResolveProvider(string? configuredProvider, string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(configuredProvider))
        {
            return configuredProvider.Trim() switch
            {
                "SqlServer" => DatabaseProvider.SqlServer,
                "sqlserver" => DatabaseProvider.SqlServer,
                "Sqlite" => DatabaseProvider.Sqlite,
                "sqlite" => DatabaseProvider.Sqlite,
                _ => throw new InvalidOperationException($"Unsupported database provider '{configuredProvider}'.")
            };
        }

        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
        {
            return DatabaseProvider.SqlServer;
        }

        return DatabaseProvider.Sqlite;
    }

    private static class DatabaseProvider
    {
        public const string Sqlite = "Sqlite";
        public const string SqlServer = "SqlServer";
    }

    private sealed class MigrationHostedService(
        IServiceProvider serviceProvider, IHostEnvironment environment, ILogger<MigrationHostedService> logger)
        : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(cancellationToken);

            var addedCities = await ReferenceDataSeeder.SeedCitiesAsync(db, cancellationToken);
            logger.LogInformation("Added {Count} missing Danish postal districts", addedCities);

            var identitySeeder = scope.ServiceProvider.GetRequiredService<AdminIdentitySeeder>();
            await identitySeeder.SeedAsync(cancellationToken);

            if (environment.IsDevelopment())
            {
                Seeder.SeedMinimal(db);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
