using InventoryAPI.Application.Common;
using Asp.Versioning;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
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
    private readonly IExcelExportService _excelExportService;
    private readonly IPdfExportService _pdfExportService;

    public AuditController(IMediator mediator, ILogger<AuditController> logger, IExcelExportService excelExportService, IPdfExportService pdfExportService)
    {
        _mediator = mediator;
        _logger = logger;
        _excelExportService = excelExportService;
        _pdfExportService = pdfExportService;
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

    /// <summary>
    /// Export audit logs to Excel
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting audit logs to Excel");

        // Fetch all audit logs without pagination
        var query = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = int.MaxValue,
            EntityType = entityType,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate,
            PerformedBy = performedBy
        };

        var result = await _mediator.Send(query, cancellationToken);

        // Generate Excel file
        var excelData = _excelExportService.ExportToExcel(result.Items, "AuditLogs");

        _logger.LogInformation("Exported {Count} audit logs to Excel", result.Items.Count);

        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"AuditLogs_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    /// <summary>
    /// Export audit logs to PDF
    /// </summary>
    [HttpGet("export/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportAuditLogsToPdf(
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? performedBy = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting audit logs to PDF");

        // Fetch all audit logs without pagination
        var query = new GetAuditLogsQuery
        {
            PageNumber = 1,
            PageSize = int.MaxValue,
            EntityType = entityType,
            Action = action,
            FromDate = fromDate,
            ToDate = toDate,
            PerformedBy = performedBy
        };

        var result = await _mediator.Send(query, cancellationToken);

        // Generate PDF file
        var pdfData = _pdfExportService.ExportToPdf(result.Items, "Audit Logs Report");

        _logger.LogInformation("Exported {Count} audit logs to PDF", result.Items.Count);

        return File(pdfData, "application/pdf",
            $"AuditLogs_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }
}
