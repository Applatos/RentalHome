using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Sommerhus.Mvc;
using Sommerhus.Mvc.ModelBinding;
using Sommerhus.Mvc.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews(options =>
    {
        // First, ahead of MVC's own number binders: they read form values with the request
        // culture, and under da-DK "800.5" from an <input type="number"> would become 8005.
        options.ModelBinderProviders.Insert(0, new CultureSafeNumberModelBinderProvider());
    })
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) => factory.Create(typeof(SharedResource));
    });

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        // Not the login page: a signed-in user who lacks a role would be sent to login, and
        // login sends signed-in users on, which looped until the browser gave up.
        options.AccessDeniedPath = "/account/access-denied";
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<AdminApiAuthHandler>();
builder.Services.AddTransient<CulturePropagationHandler>();

// API clients
// HttpClient.BaseAddress drops its last path segment when it has no trailing slash:
// "https://host/api" + "api/houses" becomes "https://host/api/houses" instead of
// "https://host/api/api/houses". Normalising here means the setting works either way.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    // Locally the API runs on a known port. Anywhere else the address depends on the server,
    // so it must be set (deploy/setup-server.ps1 puts Api__BaseUrl on the app pool) rather than
    // silently falling back to localhost and failing on every page.
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Api:BaseUrl is not configured. Set the Api__BaseUrl environment variable to the public URL of the API, e.g. https://host/api/.");
    apiBaseUrl = "http://localhost:5001/";
}
if (!apiBaseUrl.EndsWith('/'))
    apiBaseUrl += "/";

builder.Services.AddHttpClient<SommerhusApi>(http =>
{
    http.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CulturePropagationHandler>();

builder.Services.AddHttpClient<AdminAuthClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CulturePropagationHandler>();

builder.Services.AddHttpClient<PublicAuthClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
}).AddHttpMessageHandler<CulturePropagationHandler>();

builder.Services.AddHttpClient<AdminApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
    .AddHttpMessageHandler<CulturePropagationHandler>()
    .AddHttpMessageHandler<AdminApiAuthHandler>();

builder.Services.AddHttpClient<UserApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
    .AddHttpMessageHandler<CulturePropagationHandler>()
    .AddHttpMessageHandler<AdminApiAuthHandler>();


var app = builder.Build();

var supportedCultures = new[]
{
    new CultureInfo("da-DK"),
    new CultureInfo("en-GB")
};

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("da-DK"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};

localizationOptions.RequestCultureProviders =
[
    new QueryStringRequestCultureProvider(),
    new CookieRequestCultureProvider(),
    new AcceptLanguageHeaderRequestCultureProvider()
];

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseStaticFiles();
app.UseRouting();
app.UseRequestLocalization(localizationOptions);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Houses}/{action=Index}/{id?}");

app.Run();
