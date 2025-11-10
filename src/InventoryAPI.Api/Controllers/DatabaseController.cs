using Asp.Versioning;
using InventoryAPI.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// Database management and status endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseInitializationService _databaseService;
    private readonly ILogger<DatabaseController> _logger;

    public DatabaseController(
        IDatabaseInitializationService databaseService,
        ILogger<DatabaseController> logger)
    {
        _databaseService = databaseService;
        _logger = logger;
    }

    /// <summary>
    /// Get detailed database status including migration information
    /// </summary>
    /// <returns>Database status information</returns>
    [HttpGet("status")]
    [AllowAnonymous] // Public endpoint for monitoring systems
    [ProducesResponseType(typeof(DatabaseStatusResponse), 200)]
    public async Task<ActionResult<DatabaseStatusResponse>> GetStatus()
    {
        var status = await _databaseService.GetStatusAsync();

        var response = new DatabaseStatusResponse
        {
            Status = status.IsHealthy ? "Healthy" : "Unhealthy",
            CanConnect = status.CanConnect,
            CurrentMigration = status.CurrentMigration,
            TotalMigrationsApplied = status.TotalMigrationsApplied,
            PendingMigrationsCount = status.PendingMigrationsCount,
            ResponseTimeMs = status.ResponseTimeMs,
            ErrorMessage = status.ErrorMessage,
            Timestamp = DateTime.UtcNow
        };

        if (!status.IsHealthy)
        {
            return StatusCode(503, response); // Service Unavailable
        }

        return Ok(response);
    }

    /// <summary>
    /// Verify database is ready (Admin only)
    /// </summary>
    /// <returns>Verification result</returns>
    [HttpPost("verify")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(VerificationResponse), 200)]
    [ProducesResponseType(503)]
    public async Task<ActionResult<VerificationResponse>> VerifyDatabase()
    {
        _logger.LogInformation("Manual database verification requested by {User}",
            User.Identity?.Name ?? "Unknown");

        var result = await _databaseService.VerifyAsync();

        var response = new VerificationResponse
        {
            Success = result.Success,
            CanConnect = result.CanConnect,
            CurrentMigration = result.CurrentMigration,
            TotalMigrationsApplied = result.TotalMigrationsApplied,
            PendingMigrationsCount = result.PendingMigrationsCount,
            PendingMigrations = result.PendingMigrations,
            VerificationTimeMs = result.InitializationTimeMs,
            ErrorMessage = result.ErrorMessage,
            Timestamp = DateTime.UtcNow
        };

        if (!result.Success)
        {
            return StatusCode(503, response);
        }

        return Ok(response);
    }
}

/// <summary>
/// Database status response
/// </summary>
public class DatabaseStatusResponse
{
    public string Status { get; set; } = string.Empty;
    public bool CanConnect { get; set; }
    public string CurrentMigration { get; set; } = string.Empty;
    public int TotalMigrationsApplied { get; set; }
    public int PendingMigrationsCount { get; set; }
    public long ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Database verification response
/// </summary>
public class VerificationResponse
{
    public bool Success { get; set; }
    public bool CanConnect { get; set; }
    public string CurrentMigration { get; set; } = string.Empty;
    public int TotalMigrationsApplied { get; set; }
    public int PendingMigrationsCount { get; set; }
    public List<string> PendingMigrations { get; set; } = new();
    public long VerificationTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; }
}
