using InventoryAPI.Application.Commands.WorkOrders;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Queries.WorkOrders;
using InventoryAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// Work order management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<WorkOrdersController> _logger;

    public WorkOrdersController(IMediator mediator, ILogger<WorkOrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get work orders with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<WorkOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<WorkOrderDto>>> GetWorkOrders(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] WorkOrderStatus? status = null,
        [FromQuery] WorkOrderPriority? priority = null,
        [FromQuery] Guid? assignedToId = null,
        [FromQuery] Guid? requestedById = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching work orders: Page {PageNumber}, Size {PageSize}", pageNumber, pageSize);

        var query = new GetWorkOrdersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            Priority = priority,
            AssignedToId = assignedToId,
            RequestedById = requestedById,
            FromDate = fromDate,
            ToDate = toDate
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Retrieved {Count} work orders out of {TotalCount}", result.Items.Count, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Get a single work order by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkOrderDto>> GetWorkOrder(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching work order {WorkOrderId}", id);

        var query = new GetWorkOrderByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("Work order {WorkOrderId} not found", id);
            return NotFound(new { message = $"Work order {id} not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new work order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkOrderDto>> CreateWorkOrder(
        [FromBody] CreateWorkOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new work order: {Title}", command.Title);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Work order created: {OrderNumber}", result.OrderNumber);

        return CreatedAtAction(nameof(GetWorkOrder), new { id = result.Id }, result);
    }

    /// <summary>
    /// Submit a draft work order for approval
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkOrderDto>> SubmitWorkOrder(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting work order {WorkOrderId}", id);

        var command = new SubmitWorkOrderCommand { WorkOrderId = id };
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Work order {WorkOrderId} submitted", id);

        return Ok(result);
    }

    /// <summary>
    /// Approve a submitted work order
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkOrderDto>> ApproveWorkOrder(
        Guid id,
        [FromBody] ApproveWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Approving work order {WorkOrderId}, assigning to {UserId}", id, request.AssignedToId);

        var command = new ApproveWorkOrderCommand
        {
            WorkOrderId = id,
            AssignedToId = request.AssignedToId
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Work order {WorkOrderId} approved and assigned", id);

        return Ok(result);
    }

    /// <summary>
    /// Reject a submitted work order
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkOrderDto>> RejectWorkOrder(
        Guid id,
        [FromBody] RejectWorkOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rejecting work order {WorkOrderId}: {Reason}", id, request.Reason);

        var command = new RejectWorkOrderCommand
        {
            WorkOrderId = id,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Work order {WorkOrderId} rejected", id);

        return Ok(result);
    }

    /// <summary>
    /// Start work on an approved work order
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkOrderDto>> StartWorkOrder(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting work order {WorkOrderId}", id);

        var command = new StartWorkOrderCommand { WorkOrderId = id };
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Work order {WorkOrderId} started", id);

        return Ok(result);
    }

    /// <summary>
    /// Complete an in-progress work order
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkOrderDto>> CompleteWorkOrder(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing work order {WorkOrderId}", id);

        var command = new CompleteWorkOrderCommand { WorkOrderId = id };
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Work order {WorkOrderId} completed", id);

        return Ok(result);
    }

    /// <summary>
    /// Cancel a work order
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkOrderDto>> CancelWorkOrder(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling work order {WorkOrderId}", id);

        var command = new CancelWorkOrderCommand { WorkOrderId = id };
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Work order {WorkOrderId} cancelled", id);

        return Ok(result);
    }

    /// <summary>
    /// Issue items from a work order (creates stock movements)
    /// </summary>
    [HttpPost("{id:guid}/issue-items")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WorkOrderDto>> IssueItems(
        Guid id,
        [FromBody] IssueWorkOrderItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Issuing items for work order {WorkOrderId}", id);

        var command = new IssueWorkOrderItemsCommand
        {
            WorkOrderId = id,
            Items = request.Items
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Items issued for work order {WorkOrderId}", id);

        return Ok(result);
    }
}

// Request DTOs
public class ApproveWorkOrderRequest
{
    public Guid AssignedToId { get; set; }
}

public class RejectWorkOrderRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class IssueWorkOrderItemsRequest
{
    public List<IssueItemRequest> Items { get; set; } = new();
}
