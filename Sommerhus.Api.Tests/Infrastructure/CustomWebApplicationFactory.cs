using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core;
using Sommerhus.Core.Identity;

namespace Sommerhus.Api.Tests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string AdminUsername = "admin";
    private const string AdminPassword = "Sommerhus123!";
    private SqliteConnection? _conn;
    private string? _tempWebRoot;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DefaultAdmin:UserName"] = AdminUsername,
                ["DefaultAdmin:Password"] = AdminPassword,
                ["DefaultAdmin:Email"] = "admin@test.local",
                ["Jwt:Issuer"] = "Sommerhus.Api",
                ["Jwt:Audience"] = "Sommerhus.Admin",
                ["Jwt:Key"] = "TestsJwtKey_Value_1234567890ABCDEF",
                ["Jwt:AccessTokenMinutes"] = "120"
            });
        });

        builder.UseEnvironment("Testing");

        _tempWebRoot = Path.Combine(Path.GetTempPath(), "sommerhus_api_tests_wwwroot", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);
        builder.UseWebRoot(_tempWebRoot);

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.None);
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Remove the MigrationHostedService to avoid conflicts in tests
            var migrationHostedServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                     d.ImplementationType?.Name == "MigrationHostedService");
            if (migrationHostedServiceDescriptor != null)
                services.Remove(migrationHostedServiceDescriptor);

            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();

            services.AddDbContext<AppDbContext>(opt =>
            {
                opt.UseSqlite(_conn);
            });

            // Override JWT bearer validation to use test config values
            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes("TestsJwtKey_Value_1234567890ABCDEF"));
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "Sommerhus.Api",
                        ValidAudience = "Sommerhus.Admin",
                        IssuerSigningKey = key,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminIdentitySeeder>();

            // Apply migrations instead of just ensuring created
            db.Database.Migrate();

            Seeder.SeedMinimal(db);
            adminSeeder.SeedAsync(CancellationToken.None).GetAwaiter().GetResult();
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        
        var response = client.PostAsJsonAsync("admin/auth/login", new AdminLoginRequest
        {
            Username = AdminUsername,
            Password = AdminPassword
        }).GetAwaiter().GetResult();

        response.EnsureSuccessStatusCode();
        var token = response.Content.ReadFromJsonAsync<AdminTokenResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Unable to deserialize admin login response.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
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
        catch { /* ignore cleanup failures */ }
    }
}
