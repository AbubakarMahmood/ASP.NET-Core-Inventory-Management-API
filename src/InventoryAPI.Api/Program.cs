// Build: 2025-11-10T02:43:40Z
using System.Reflection;
using System.Text;
using FluentValidation;
using InventoryAPI.Api.Middleware;
using InventoryAPI.Api.Services;
using InventoryAPI.Application.Behaviors;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Mappings;
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

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/inventory-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// SignalR
builder.Services.AddSignalR();

// HttpContextAccessor (needed for accessing current user in handlers)
builder.Services.AddHttpContextAccessor();

// Data Protection - Persist keys to a directory that survives container restarts
var dataProtectionPath = Path.Combine("/app", "data-protection-keys");
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
        // Enable automatic retry on transient failures
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);

        // Command timeout for long-running migrations
        npgsqlOptions.CommandTimeout(120);
    });
});

// Register IApplicationDbContext interface for Clean Architecture
builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

// Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(MappingProfile).Assembly);
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(MappingProfile).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
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
});

builder.Services.AddAuthorization();

// CORS (with SignalR support)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });

    // Additional policy for SignalR (needs credentials)
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5001", "https://localhost:5001")
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
        Description = "A production-grade REST API for inventory and work order management",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@inventory.com"
        }
    });

    // Add JWT Authentication to Swagger
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

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Health checks with detailed database status
builder.Services.AddHealthChecks()
    .AddCheck<InventoryAPI.Api.HealthChecks.DatabaseHealthCheck>(
        "database",
        tags: new[] { "db", "sql", "ready" });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1");
        c.RoutePrefix = "swagger";
    });
}

// Initialize database with environment-aware strategy
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var databaseService = services.GetRequiredService<IDatabaseInitializationService>();
    var environment = services.GetRequiredService<IWebHostEnvironment>();

    try
    {
        if (environment.IsDevelopment())
        {
            Log.Information("=== DEVELOPMENT MODE: Initializing database ===");
            var result = await databaseService.InitializeAsync();

            if (result.Success)
            {
                Log.Information("✓ Database initialization successful");
                Log.Information("  - Connection: {Status}", result.CanConnect ? "Connected" : "Failed");
                Log.Information("  - Current Migration: {Migration}", result.CurrentMigration);
                Log.Information("  - Total Migrations Applied: {Count}", result.TotalMigrationsApplied);
                if (result.MigrationsApplied)
                {
                    Log.Information("  - New Migrations Applied: {Count}", result.PendingMigrationsCount);
                }
                if (result.DataSeeded)
                {
                    Log.Information("  - Seed Data: Processed");
                }
                Log.Information("  - Initialization Time: {Time}ms", result.InitializationTimeMs);
            }
            else
            {
                Log.Warning("⚠ Database initialization completed with warnings");
                Log.Warning("  - Error: {Error}", result.ErrorMessage ?? "Unknown");
                Log.Warning("  - Application will start but database functionality may be limited");
            }
        }
        else
        {
            Log.Information("=== PRODUCTION MODE: Verifying database ===");
            var result = await databaseService.VerifyAsync();

            if (!result.Success)
            {
                Log.Fatal("✗ PRODUCTION STARTUP BLOCKED: Database verification failed");
                Log.Fatal("  - Error: {Error}", result.ErrorMessage ?? "Unknown");
                Log.Fatal("  - Action Required: Apply pending migrations before starting the application");

                if (result.PendingMigrations.Any())
                {
                    Log.Fatal("  - Pending Migrations: {Migrations}",
                        string.Join(", ", result.PendingMigrations));
                    Log.Fatal("  - Run: dotnet ef database update --project src/InventoryAPI.Infrastructure --startup-project src/InventoryAPI.Api");
                }

                throw new InvalidOperationException(
                    "Database is not ready for production. " +
                    "Apply all pending migrations before starting the application.");
            }

            Log.Information("✓ Database verification successful");
            Log.Information("  - Current Migration: {Migration}", result.CurrentMigration);
            Log.Information("  - Total Migrations: {Count}", result.TotalMigrationsApplied);
            Log.Information("  - Pending Migrations: {Count}", result.PendingMigrationsCount);
            Log.Information("  - Verification Time: {Time}ms", result.InitializationTimeMs);
        }
    }
    catch (Exception ex) when (environment.IsProduction())
    {
        // In production, fail fast if database is not ready
        Log.Fatal(ex, "FATAL: Production startup failed due to database issues");
        throw;
    }
    catch (Exception ex)
    {
        // In development, log but continue (for better DX)
        Log.Error(ex, "Database initialization encountered an error, but application will continue");
    }
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<InventoryAPI.Api.Hubs.NotificationHub>("/api/v1/notifications");

app.MapHealthChecks("/api/v1/health");

Log.Information("Starting Inventory Management API");

app.Run();
