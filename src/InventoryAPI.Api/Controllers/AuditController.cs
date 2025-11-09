using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Queries.Audit;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// Audit log endpoints for tracking system changes
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin,Manager")]
public class AuditController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IMediator mediator, ILogger<AuditController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get audit logs with optional filtering
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <param name="entityType">Filter by entity type (Product, WorkOrder, User, StockMovement)</param>
    /// <param name="action">Filter by action (Created, Modified, Deleted)</param>
    /// <param name="fromDate">Filter by start date</param>
    /// <param name="toDate">Filter by end date</param>
    /// <param name="performedBy">Filter by user who performed the action</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<AuditLogDto>>> GetAuditLogs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching audit logs: Page {PageNumber}, Size {PageSize}, EntityType: {EntityType}, Action: {Action}",
            pageNumber, pageSize, entityType, action);

        var query = new GetAuditLogsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            EntityType = entityType,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate,
            PerformedBy = performedBy
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Retrieved {Count} audit logs out of {TotalCount}",
            result.Items.Count, result.TotalCount);

        return Ok(result);
    }
}
