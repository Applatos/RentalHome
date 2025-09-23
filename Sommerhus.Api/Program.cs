using Microsoft.OpenApi.Models;
using Sommerhus.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Sommerhus.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Db
        builder.Services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseSqlite(builder.Configuration.GetConnectionString("Default")
                ?? "Data Source=sommerhus.db");
        });

        builder.Services.AddControllers();

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
                .SetIsOriginAllowed(_ => true)
                .AllowCredentials());
        });

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseCors("mvc");

        app.UseMiddleware<ProblemDetailsMiddleware>();


        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapControllers();

        // Migration + seed
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
            Seeder.SeedMinimal(db);
        }

        await app.RunAsync();
    }
}
