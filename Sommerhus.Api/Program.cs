using Microsoft.OpenApi.Models;
using Sommerhus.Api.Data;
using Microsoft.EntityFrameworkCore;

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

// CORS – tillad MVC app (justér ved behov)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("mvc", p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(_ => true) // simplere under dev; stram evt. op
        .AllowCredentials());
});

var app = builder.Build();

app.UseStaticFiles(); // wwwroot (uploads)
app.UseCors("mvc");


app.UseSwagger();
app.UseSwaggerUI();


app.MapControllers();

// Migration + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
    DbSeeder.Seed_cities(db);
}

app.Run();
