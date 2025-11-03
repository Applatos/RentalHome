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

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddHostedService<MigrationHostedService>();

        return services;
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
