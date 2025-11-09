using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sommerhus.Repository;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSommerhusPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=sommerhus.db";
        var provider = ResolveProvider(configuration["DatabaseProvider"], connectionString);

        services.AddDbContext<AppDbContext>(options =>
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
        });

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

    private sealed class MigrationHostedService(IServiceProvider serviceProvider, IHostEnvironment environment)
        : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await db.Database.MigrateAsync(cancellationToken);

            if (environment.IsDevelopment())
            {
                Seeder.SeedMinimal(db);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
