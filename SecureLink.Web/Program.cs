using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using SecureLink.Web.Services;
using Serilog;
using System.Collections.Concurrent;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Filter.ByExcluding(logEvent =>
        logEvent.Properties.TryGetValue("SourceContext", out var source) &&
        (
            source.ToString().Contains("Microsoft.Hosting.Lifetime") ||
            source.ToString().Contains("Microsoft.AspNetCore.Server.Kestrel") ||
            source.ToString().Contains("Microsoft.AspNetCore.Routing") ||
            source.ToString().Contains("Microsoft.AspNetCore.SignalR") ||
            source.ToString().Contains("Microsoft.AspNetCore.Http.Connections") ||
            source.ToString().Contains("Microsoft.AspNetCore.Diagnostics") ||  // Chybové stránky
            source.ToString().Contains("Microsoft.AspNetCore.StaticFiles")      // Statické soubory
        )
    )
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<CaptchaService>();

builder.Services.AddScoped<ProtectedSessionStorage>();

builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<Program>>();

    var baseUrl = config["API:BaseUrl"];
    logger.LogInformation("Loaded API BaseUrl: {BaseUrl}", baseUrl);

    if (string.IsNullOrEmpty(baseUrl))
    {
        logger.LogError("API BaseUrl is missing in configuration.");
        throw new Exception("API BaseUrl is missing in configuration.");
    }

    return new HttpClient { BaseAddress = new Uri(baseUrl) };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuthConfigService>();
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<SecureLinkService>();
builder.Services.AddScoped<BlazorBootstrap.ModalService>();

builder.Services.AddSingleton<CaptchaRateLimiterService>();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

var rateLimiter = new ConcurrentDictionary<string, (DateTime Timestamp, int Count)>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
