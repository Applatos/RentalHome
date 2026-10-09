using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sommerhus.Core;
using Sommerhus.Core.Data;
using Sommerhus.Core.Dtos.Admin;

namespace Sommerhus.Api.Tests.Infrastructure;

/// <summary>
/// A "Production" host on a SQLite file and web root inside <paramref name="directory"/>. Unlike
/// the shared factory, this retains the application's hosted services and never invokes
/// migrations or seeders itself. Starting the host must provision its own data.
/// </summary>
internal sealed class ProductionFactory(string directory, string adminUsername, string adminPassword)
    : WebApplicationFactory<Program>
{
    public string WebRoot => Path.Combine(directory, "wwwroot");

    public HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        using var login = client.PostAsJsonAsync("/api/admin/auth/login", new AdminLoginRequest
        {
            Username = adminUsername,
            Password = adminPassword
        }).GetAwaiter().GetResult();
        login.EnsureSuccessStatusCode();
        var token = login.Content.ReadFromJsonAsync<AdminTokenResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Unable to deserialize admin login response.");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(WebRoot);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(directory, "startup.db"),
            Pooling = false
        }.ToString();

        builder.UseEnvironment("Production");
        builder.UseWebRoot(WebRoot);
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite",
                ["ConnectionStrings:Default"] = connectionString,
                ["DefaultAdmin:UserName"] = adminUsername,
                ["DefaultAdmin:Password"] = adminPassword,
                ["DefaultAdmin:Email"] = $"{adminUsername}@test.local",
                ["Jwt:Issuer"] = "Sommerhus.StartupTests",
                ["Jwt:Audience"] = "Sommerhus.StartupTests.Admin",
                ["Jwt:Key"] = "StartupTestsOnly_Key_1234567890ABCDEF",
                ["Jwt:AccessTokenMinutes"] = "10",
                ["Storage:UploadsPath"] = "uploads"
            }));
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            // Persistence captures configuration before the test host's configuration
            // callback. Replace only its options to guarantee an isolated test database.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddScoped(sp => new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connectionString)
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>())
                .Options);
        });
    }
}
