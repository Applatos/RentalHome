using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sommerhus.Core.Dtos.Admin;
using Sommerhus.Core.Dtos.Auth;

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

            // Remove existing interceptor registration from AddSommerhusPersistence
            var interceptorDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Sommerhus.Core.Data.AuditSaveChangesInterceptor));
            if (interceptorDescriptor != null)
                services.Remove(interceptorDescriptor);

            // Remove the MigrationHostedService to avoid conflicts in tests
            var migrationHostedServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                     d.ImplementationType?.Name == "MigrationHostedService");
            if (migrationHostedServiceDescriptor != null)
                services.Remove(migrationHostedServiceDescriptor);

            // The nightly from-price refresh would run against the test database while tests do.
            var refreshDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService) &&
                     d.ImplementationType == typeof(Sommerhus.Core.Services.Public.Pricing.PriceSummaryRefreshService));
            if (refreshDescriptor != null)
                services.Remove(refreshDescriptor);

            _conn = new SqliteConnection("DataSource=:memory:");
            _conn.Open();

            services.AddDbContext<AppDbContext>((sp, opt) =>
            {
                opt.UseSqlite(_conn);
                opt.AddInterceptors(sp.GetRequiredService<Sommerhus.Core.Data.AuditSaveChangesInterceptor>());
            });

            services.AddScoped<Sommerhus.Core.Data.AuditSaveChangesInterceptor>();

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
        
        var response = client.PostAsJsonAsync("api/admin/auth/login", new AdminLoginRequest
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

    public HttpClient CreateUserClient(string username = "testuser", string password = "TestUser123!")
    {
        var client = CreateClient();

        // Register via public API
        var regResponse = client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = $"{username}@test.local",
            Password = password,
            FirstName = "Test",
            LastName = "User"
        }).GetAwaiter().GetResult();
        regResponse.EnsureSuccessStatusCode();

        var token = regResponse.Content.ReadFromJsonAsync<AuthTokenResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Unable to deserialize register response.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return client;
    }

    public HttpClient CreateOwnerClient(string username = "testowner", string password = "TestOwner123!")
    {
        var client = CreateClient();

        // Register via public API
        var regResponse = client.PostAsJsonAsync("api/auth/register", new RegisterRequest
        {
            Username = username,
            Email = $"{username}@test.local",
            Password = password,
            FirstName = "Test",
            LastName = "Owner"
        }).GetAwaiter().GetResult();
        regResponse.EnsureSuccessStatusCode();

        var token = regResponse.Content.ReadFromJsonAsync<AuthTokenResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Unable to deserialize register response.");

        // Promote to HouseOwner via admin
        var adminClient = CreateAuthenticatedClient();
        var userId = GetUserIdFromToken(token.Token);

        adminClient.PutAsJsonAsync($"api/admin/users/{userId}/role",
            new ChangeUserRoleRequest { Role = AppRoles.HouseOwner })
            .GetAwaiter().GetResult().EnsureSuccessStatusCode();

        // Re-login to get updated token with HouseOwner role
        var loginResponse = client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Username = username,
            Password = password
        }).GetAwaiter().GetResult();
        loginResponse.EnsureSuccessStatusCode();

        var ownerToken = loginResponse.Content.ReadFromJsonAsync<AuthTokenResponse>().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Unable to deserialize login response.");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken.Token);
        return client;
    }

    private static string GetUserIdFromToken(string token)
    {
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims.First(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier
            || c.Type == "sub").Value;
    }

    public void EnsureHousePublished(Guid houseId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var house = db.Houses.FirstOrDefault(h => h.Id == houseId);
        if (house is not null && house.Status != Sommerhus.Domain.Models.EntityStatus.Published)
        {
            house.Status = Sommerhus.Domain.Models.EntityStatus.Published;
            house.PublishedAtUtc = DateTime.UtcNow;
            db.SaveChanges();
        }
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
