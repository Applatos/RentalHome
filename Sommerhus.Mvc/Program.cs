using Sommerhus.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// API client
builder.Services.AddHttpClient<ISommerhusApi, SommerhusApi>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5001/");
});

builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:5001/";
    client.BaseAddress = new Uri(baseUrl);
});


var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
