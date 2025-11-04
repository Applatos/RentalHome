using Microsoft.AspNetCore.Authentication.Cookies;
using Sommerhus.Mvc.Infrastructure;
using Sommerhus.Mvc.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddOptions<AdminAuthOptions>()
    .BindConfiguration(AdminAuthOptions.SectionName)
    .ValidateOnStart();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AdminApiAuthHandler>();

// API clients
builder.Services.AddHttpClient<SommerhusApi>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5001/");
});

builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    var baseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5001/";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<AdminApiAuthHandler>();


var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Houses}/{action=Index}/{id?}");

app.Run();
