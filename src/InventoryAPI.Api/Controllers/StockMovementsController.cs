using InventoryAPI.Application.Common;
using Asp.Versioning;
using InventoryAPI.Application.Commands.StockMovements;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Queries.StockMovements;
using InventoryAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// Stock movements management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class StockMovementsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StockMovementsController> _logger;

    public StockMovementsController(IMediator mediator, ILogger<StockMovementsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get stock movements with optional filtering
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <param name="productId">Filter by product ID</param>
    /// <param name="type">Filter by movement type (Receipt, Issue, Adjustment, Transfer, Return)</param>
    /// <param name="fromDate">Filter movements from this date</param>
    /// <param name="toDate">Filter movements to this date</param>
    /// <returns>Paginated list of stock movements</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<StockMovementDto>>> GetStockMovements(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? productId = null,
        [FromQuery] StockMovementType? type = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching stock movements: Page {PageNumber}, Size {PageSize}, ProductId {ProductId}, Type {Type}",
            pageNumber, pageSize, productId, type);

        var query = new GetStockMovementsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ProductId = productId,
            Type = type,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} stock movements out of {TotalCount} total",
            result.Items.Count, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Get stock movements for a specific product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated list of stock movements for the product</returns>
    [HttpGet("product/{productId:guid}")]
    [ProducesResponseType(typeof(PaginatedResult<StockMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<StockMovementDto>>> GetProductStockMovements(
        Guid productId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching stock movements for product {ProductId}: Page {PageNumber}, Size {PageSize}",
            productId, pageNumber, pageSize);

        var query = new GetStockMovementsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            ProductId = productId
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} stock movements for product {ProductId}",
            result.TotalCount, productId);

        return Ok(result);
    }

    /// <summary>
    /// Record a new stock movement
    /// </summary>
    /// <param name="command">Stock movement details</param>
    /// <returns>Created stock movement</returns>
    [HttpPost]
    [ProducesResponseType(typeof(StockMovementDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockMovementDto>> RecordStockMovement(
        [FromBody] RecordStockMovementCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Recording stock movement: Product {ProductId}, Type {Type}, Quantity {Quantity}",
            command.ProductId, command.Type, command.Quantity);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation(
            "Stock movement recorded successfully: {MovementId}",
            result.Id);

        return CreatedAtAction(
            nameof(GetStockMovements),
            new { id = result.Id },
            result);
    }

    /// <summary>
    /// Get summary statistics for stock movements
    /// </summary>
    /// <param name="fromDate">Start date for statistics</param>
    /// <param name="toDate">End date for statistics</param>
    /// <returns>Stock movement statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetStockMovementStatistics(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Fetching stock movement statistics from {FromDate} to {ToDate}",
            fromDate, toDate);

        // Get all movements in date range
        var query = new GetStockMovementsQuery
        {
            PageNumber = 1,
            PageSize = 10000, // Large number to get all for statistics
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query, cancellationToken);

        // Calculate statistics
        var statistics = new
        {
            TotalMovements = result.TotalCount,
            ReceiptCount = result.Items.Count(m => m.Type == StockMovementType.Receipt),
            IssueCount = result.Items.Count(m => m.Type == StockMovementType.Issue),
            AdjustmentCount = result.Items.Count(m => m.Type == StockMovementType.Adjustment),
            TransferCount = result.Items.Count(m => m.Type == StockMovementType.Transfer),
            ReturnCount = result.Items.Count(m => m.Type == StockMovementType.Return),
            TotalQuantityIn = result.Items
                .Where(m => m.Type == StockMovementType.Receipt || m.Type == StockMovementType.Return)
                .Sum(m => m.Quantity),
            TotalQuantityOut = result.Items
                .Where(m => m.Type == StockMovementType.Issue)
                .Sum(m => m.Quantity),
            UniqueProducts = result.Items.Select(m => m.ProductId).Distinct().Count(),
            FromDate = fromDate ?? result.Items.Min(m => m.Timestamp),
            ToDate = toDate ?? result.Items.Max(m => m.Timestamp)
        };

        _logger.LogInformation(
            "Stock movement statistics calculated: {TotalMovements} movements",
            statistics.TotalMovements);

        return Ok(statistics);
    }
}
