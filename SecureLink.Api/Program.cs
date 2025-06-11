using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SecureLink.Api.Data.Core;
using SecureLink.Api.Data.Interfaces;
using SecureLink.Api.Data;
using SecureLink.Api.Services;
using Serilog;
using System.Text;
using SecureLink.Api;
using SecureLink.Web.Services;

var builder = WebApplication.CreateBuilder(args);

//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.ListenAnyIP(80, listenOptions =>
//    {
//        listenOptions.UseConnectionLogging();
//    });

//    options.ListenAnyIP(443, listenOptions =>
//    {
//        listenOptions.UseHttps();
//    });
//});

var configuration = builder.Configuration;
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSingleton<DatabaseConnectionFactory>();

builder.Services.AddScoped<IJwtKeyDataAccess, JwtKeyDataAccess>();
builder.Services.AddScoped<IUserDataAccess, UserDataAccess>();
builder.Services.AddScoped<IKeyAssigmentDataAccess, KeyAssigmentDataAccess>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();

builder.Services.AddScoped<JwtKeyCache>();
builder.Services.AddScoped<JwtKeyManager>();
builder.Services.AddScoped<UserManager>();
builder.Services.AddScoped<LogManager>();
builder.Services.AddScoped<LinkService>();
builder.Services.AddScoped<HttpClient>();

builder.Services.AddSingleton<NonceCache>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "AK System",
            ValidAudience = "AK System",
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var sp = validationParameters.RequireSignedTokens ? builder.Services.BuildServiceProvider() : null;
                var jwtKeyManager = sp?.GetRequiredService<JwtKeyManager>();
                var activeKeys = jwtKeyManager?.GetValidSigningKeysAsync().GetAwaiter().GetResult();

                if (activeKeys == null || !activeKeys.Any())
                    throw new SecurityTokenException("No valid JWT keys available.");

                return activeKeys.Select(k => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(k.KeyValue))).ToList();
            }
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var ipAddress = context.HttpContext.Connection.RemoteIpAddress;
                Log.Logger.Warning("Authentication failed from IP {IP}: {Error}", ipAddress, context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                if (!context.Handled)
                {
                    var ipAddress = context.HttpContext.Connection.RemoteIpAddress;
                    Log.Logger.Warning("Unauthorized request from IP {IP}: Path {Path}, Method {Method}",
                        ipAddress,
                        context.HttpContext.Request.Path,
                        context.HttpContext.Request.Method);
                }
                return Task.CompletedTask;
            }
        };
    });


// Konfigurace Swaggeru
bool enableSwagger = configuration.GetValue<bool>("EnableSwagger");
if (enableSwagger)
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Version = "v1",
            Title = "Secure Link API",
            Description = "API pro propojení odkazu s Pohodou",
        });
    });
}

var app = builder.Build();

// Middleware pro Swagger, pokud je povolen
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Licenční server API v1");
    });
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
