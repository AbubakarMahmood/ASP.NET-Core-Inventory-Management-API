using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Models;
using InventoryAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryAPI.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API pipeline against an isolated in-memory database.
/// Each factory instance gets its own database so tests do not interfere.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"InventoryTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.UseSetting("JwtSettings:SecretKey", new string('x', 64));
        builder.UseSetting("JwtSettings:Issuer", "InventoryAPI");
        builder.UseSetting("JwtSettings:Audience", "InventoryAPIUsers");
        builder.UseSetting("JwtSettings:ExpiryMinutes", "60");
        builder.UseSetting("JwtSettings:RefreshTokenExpiryDays", "7");

        builder.ConfigureServices(services =>
        {
            // Swap PostgreSQL for the in-memory provider
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                            d.ServiceType == typeof(ApplicationDbContext))
                .ToList();
            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // The real initializer talks migrations, which the in-memory
            // provider does not support.
            var initDescriptor = services.Single(d => d.ServiceType == typeof(IDatabaseInitializationService));
            services.Remove(initDescriptor);
            services.AddScoped<IDatabaseInitializationService, InMemoryDatabaseInitializer>();
        });
    }

    private sealed class InMemoryDatabaseInitializer : IDatabaseInitializationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordService _passwordService;

        public InMemoryDatabaseInitializer(ApplicationDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public async Task<DatabaseInitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            await _context.Database.EnsureCreatedAsync(cancellationToken);
            await DatabaseSeeder.SeedAsync(_context, _passwordService);

            return new DatabaseInitializationResult
            {
                Success = true,
                CanConnect = true,
                DataSeeded = true,
                CurrentMigration = "InMemory"
            };
        }

        public Task<DatabaseInitializationResult> VerifyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DatabaseInitializationResult { Success = true, CanConnect = true });

        public Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DatabaseStatus { CanConnect = true, IsHealthy = true, CurrentMigration = "InMemory" });
    }
}
