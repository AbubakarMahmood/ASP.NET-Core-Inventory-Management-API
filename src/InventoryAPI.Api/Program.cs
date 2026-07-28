using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using InventoryAPI.Api.Middleware;
using InventoryAPI.Api.Services;
using InventoryAPI.Application.Behaviors;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Infrastructure.Data;
using InventoryAPI.Infrastructure.Repositories;
using InventoryAPI.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_048_576;
});

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/stockverity-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.MaxDepth = 32;
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
        // Accept and emit enum names ("Receipt") rather than ambiguous bare numbers.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    });

builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

// Data protection keys persist across restarts (path is configurable for containers)
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("StockVerity");

// Database with connection resilience
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
    }

    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);

        npgsqlOptions.CommandTimeout(120);
    });
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddSingleton<IRefreshTokenHasher, RefreshTokenHasher>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ValidationBehavior<,>).Assembly);
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(ValidationBehavior<,>).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(secretKey))
{
    throw new InvalidOperationException(
        "JwtSettings:SecretKey is not configured. Set it via configuration or the JwtSettings__SecretKey environment variable.");
}
if (secretKey.Length < 32)
{
    throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 characters.");
}

var jwtIssuer = jwtSettings["Issuer"];
var jwtAudience = jwtSettings["Audience"];
if (string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JwtSettings:Issuer and JwtSettings:Audience are required.");
}

if (!int.TryParse(jwtSettings["ExpiryMinutes"], out var accessTokenMinutes)
    || accessTokenMinutes is < 1 or > 1_440)
{
    throw new InvalidOperationException("JwtSettings:ExpiryMinutes must be between 1 and 1440.");
}

if (!int.TryParse(jwtSettings["RefreshTokenExpiryDays"], out var refreshTokenDays)
    || refreshTokenDays is < 1 or > 90)
{
    throw new InvalidOperationException("JwtSettings:RefreshTokenExpiryDays must be between 1 and 90.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Keep the original JWT claim names (sub, email) instead of remapping
    // them to the legacy SOAP-style claim types.
    options.MapInboundClaims = false;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Allow SignalR websocket connections to authenticate via query string,
    // since browsers cannot set headers on websocket requests.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/api/v1/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

// CORS: origins come from configuration so deployments can restrict them
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "http://localhost:5001", "https://localhost:5001" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StockVerity API",
        Version = "v1",
        Description = "Auditable single-location inventory ledger and work-order fulfilment API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT access token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<InventoryAPI.Api.HealthChecks.DatabaseHealthCheck>(
        "database",
        tags: new[] { "db", "sql", "ready" });

var app = builder.Build();

var openApiEnabled = builder.Configuration.GetValue<bool>("OpenApi:Enabled");
if (openApiEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "StockVerity API v1");
        options.RoutePrefix = "swagger";
    });
}

// Database startup is fail-closed in every environment. Applying migrations and
// seeding demo data are explicit configuration choices, never environment side effects.
using (var scope = app.Services.CreateScope())
{
    var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
    var result = await databaseService.InitializeAsync();

    if (!result.Success)
    {
        if (result.PendingMigrations.Any())
        {
            Log.Fatal(
                "Pending migrations: {Migrations}. Enable Database:ApplyMigrations for an intentional demo deployment, " +
                "or apply them before starting the API.",
                string.Join(", ", result.PendingMigrations));
        }

        throw new InvalidOperationException(
            $"Database is not ready: {result.ErrorMessage ?? "unknown error"}.");
    }

    Log.Information(
        "Database ready. Migration: {Migration}, applied: {Applied}, seeded: {Seeded}, took {Time}ms",
        result.CurrentMigration, result.TotalMigrationsApplied, result.DataSeeded, result.InitializationTimeMs);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

var httpsRedirectionEnabled = builder.Configuration.GetValue(
    "HttpsRedirection:Enabled",
    !app.Environment.IsDevelopment());
if (httpsRedirectionEnabled)
{
    app.UseHttpsRedirection();
}

app.UseCors("Default");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", IResult () =>
    openApiEnabled
        ? Results.Redirect("/swagger")
        : Results.Ok(new { service = "StockVerity API", status = "running" }))
    .ExcludeFromDescription();

app.MapHub<InventoryAPI.Api.Hubs.NotificationHub>("/api/v1/notifications");

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapHealthChecks("/api/v1/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

Log.Information("Starting StockVerity API");

app.Run();

public partial class Program;
