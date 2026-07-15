using System.Reflection;
using System.Text;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/inventory-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Accept and emit enum names ("Receipt") rather than bare numbers
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddSignalR();

builder.Services.AddHttpContextAccessor();

// Data protection keys persist across restarts (path is configurable for containers)
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys");
Directory.CreateDirectory(dataProtectionPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("InventoryAPI");

// Database with connection resilience
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
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
        Title = "Inventory Management API",
        Version = "v1",
        Description = "REST API for inventory and work order management"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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

// Swagger is enabled in all environments: this API's deployments are demos
// and the interactive documentation is part of the product.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
    c.RoutePrefix = "swagger";
});

// Database startup strategy:
//  - Development: apply migrations and seed automatically.
//  - Production: verify the schema is current and fail fast if it is not.
using (var scope = app.Services.CreateScope())
{
    var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();

    try
    {
        if (app.Environment.IsDevelopment())
        {
            var result = await databaseService.InitializeAsync();

            if (result.Success)
            {
                Log.Information(
                    "Database ready. Migration: {Migration}, applied: {Applied}, seeded: {Seeded}, took {Time}ms",
                    result.CurrentMigration, result.TotalMigrationsApplied, result.DataSeeded, result.InitializationTimeMs);
            }
            else
            {
                Log.Warning("Database initialization failed: {Error}. The API will start but may not function.",
                    result.ErrorMessage ?? "Unknown");
            }
        }
        else
        {
            var result = await databaseService.VerifyAsync();

            if (!result.Success)
            {
                if (result.PendingMigrations.Any())
                {
                    Log.Fatal("Pending migrations: {Migrations}. Apply them before starting: " +
                              "dotnet ef database update --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api",
                        string.Join(", ", result.PendingMigrations));
                }

                throw new InvalidOperationException(
                    $"Database is not ready: {result.ErrorMessage ?? "unknown error"}. " +
                    "Apply all pending migrations before starting the application.");
            }

            Log.Information("Database verified. Migration: {Migration}", result.CurrentMigration);
        }
    }
    catch (Exception ex) when (!app.Environment.IsDevelopment())
    {
        Log.Fatal(ex, "Startup aborted: database is not ready");
        throw;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database initialization failed; continuing so the failure can be diagnosed via /api/v1/health");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<InventoryAPI.Api.Hubs.NotificationHub>("/api/v1/notifications");

app.MapHealthChecks("/api/v1/health");

Log.Information("Starting Inventory Management API");

app.Run();

public partial class Program;
