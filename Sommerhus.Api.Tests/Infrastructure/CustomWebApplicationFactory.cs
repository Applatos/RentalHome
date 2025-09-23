using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sommerhus.Api.Data;

namespace Sommerhus.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _conn;
    private string? _tempWebRoot;
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Isoleret wwwroot til uploads i tests
        _tempWebRoot = Path.Combine(Path.GetTempPath(), "sommerhus_api_tests_wwwroot", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);
        builder.UseWebRoot(_tempWebRoot);


        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();

            // globalt: kun fejl
            logging.SetMinimumLevel(LogLevel.Information);

            // specifikt for EF Core: slå helt ned
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.None);
        });

        builder.ConfigureServices(services =>
        {
            // Fjern eksisterende DbContext
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Opret én persistent connection til shared in-memory
            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();

            // Registrer DbContext med den connection
            services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlite(connection);
            });

            // Byg provider og migrer + seed
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();  // kør migrations på den åbne connection

            Seeder.SeedMinimal(db);
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (_conn is not null)
            {
                _conn.Close();
                _conn.Dispose();
            }

            if (!string.IsNullOrWhiteSpace(_tempWebRoot) && Directory.Exists(_tempWebRoot))
                Directory.Delete(_tempWebRoot, true);
        }
        catch { /* no-throw on cleanup */ }
    }
}
