using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Mappings;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Commands.WorkOrders;

public class SubmitWorkOrderCommandHandler : IRequestHandler<SubmitWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public SubmitWorkOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(SubmitWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await GetWorkOrderAsync(_unitOfWork, request.WorkOrderId, cancellationToken);
        workOrder.Submit();
        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Submitted",
            $"Work order {workOrder.OrderNumber} has been submitted for approval");
        return workOrder.ToDto();
    }

    internal static async Task<WorkOrder> GetWorkOrderAsync(
        IUnitOfWork unitOfWork,
        Guid id,
        CancellationToken cancellationToken) =>
        await unitOfWork.WorkOrders.GetByIdWithDetailsAsync(id, cancellationToken)
        ?? throw new NotFoundException(nameof(WorkOrder), id);
}

public class ApproveWorkOrderCommandHandler : IRequestHandler<ApproveWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public ApproveWorkOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(ApproveWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await SubmitWorkOrderCommandHandler.GetWorkOrderAsync(
            _unitOfWork, request.WorkOrderId, cancellationToken);
        var assignedUser = await _unitOfWork.Users.GetByIdAsync(request.AssignedToId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.AssignedToId);

        if (!assignedUser.IsActive)
        {
            throw new BusinessRuleViolationException("Work orders can only be assigned to active users.");
        }

        workOrder.Approve(request.AssignedToId);
        workOrder.AssignedTo = assignedUser;
        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Approved",
            $"Work order {workOrder.OrderNumber} has been approved and assigned to {assignedUser.FullName}");
        return workOrder.ToDto();
    }
}

public class RejectWorkOrderCommandHandler : IRequestHandler<RejectWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public RejectWorkOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(RejectWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await SubmitWorkOrderCommandHandler.GetWorkOrderAsync(
            _unitOfWork, request.WorkOrderId, cancellationToken);
        workOrder.Reject(TextNormalization.Required(request.Reason));
        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Rejected",
            $"Work order {workOrder.OrderNumber} has been rejected: {workOrder.RejectionReason}");
        return workOrder.ToDto();
    }
}

public class StartWorkOrderCommandHandler : IRequestHandler<StartWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public StartWorkOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(StartWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await SubmitWorkOrderCommandHandler.GetWorkOrderAsync(
            _unitOfWork, request.WorkOrderId, cancellationToken);
        workOrder.Start();
        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Started",
            $"Work order {workOrder.OrderNumber} is now in progress");
        return workOrder.ToDto();
    }
}

public class CompleteWorkOrderCommandHandler : IRequestHandler<CompleteWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public CompleteWorkOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(CompleteWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await SubmitWorkOrderCommandHandler.GetWorkOrderAsync(
            _unitOfWork, request.WorkOrderId, cancellationToken);
        workOrder.Complete(DateTime.UtcNow);
        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Completed",
            $"Work order {workOrder.OrderNumber} has been completed");
        return workOrder.ToDto();
    }
}

public class CancelWorkOrderCommandHandler : IRequestHandler<CancelWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public CancelWorkOrderCommandHandler(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<WorkOrderDto> Handle(CancelWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await SubmitWorkOrderCommandHandler.GetWorkOrderAsync(
            _unitOfWork, request.WorkOrderId, cancellationToken);
        workOrder.Cancel();
        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Cancelled",
            $"Work order {workOrder.OrderNumber} has been cancelled");
        return workOrder.ToDto();
    }
}

/// <summary>
/// Atomically validates and applies an idempotent issue batch. Every requested
/// line is checked before tracked state is mutated; each product gets one
/// append-only movement carrying the shared operation id and historical balance
/// snapshots.
/// </summary>
public class IssueWorkOrderItemsCommandHandler : IRequestHandler<IssueWorkOrderItemsCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public IssueWorkOrderItemsCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<WorkOrderDto> Handle(
        IssueWorkOrderItemsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            throw new ValidationException("Items", "At least one item must be issued.");
        }

        var workOrder = await SubmitWorkOrderCommandHandler.GetWorkOrderAsync(
            _unitOfWork, request.WorkOrderId, cancellationToken);

        var priorMovements = (await _unitOfWork.StockMovements.FindAsync(
            movement => movement.OperationId == request.OperationId,
            cancellationToken)).ToList();

        if (priorMovements.Count > 0)
        {
            if (!IsReplayOf(priorMovements, request, workOrder))
            {
                throw new IdempotencyConflictException(request.OperationId);
            }

            return workOrder.ToDto();
        }

        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            throw new BusinessRuleViolationException(
                "Only in-progress work orders can have items issued.");
        }

        var duplicate = request.Items
            .GroupBy(item => item.ProductId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null)
        {
            throw new ValidationException(
                "Items",
                $"Product {duplicate.Key} may appear only once in an issue request.");
        }

        var issuePlan = new List<(WorkOrderItem Item, IssueItemRequest Request, string Reason)>();
        foreach (var issueRequest in request.Items)
        {
            var item = workOrder.Items.FirstOrDefault(candidate => candidate.ProductId == issueRequest.ProductId)
                ?? throw new NotFoundException(
                    $"Product {issueRequest.ProductId} is not part of work order {workOrder.OrderNumber}.");

            if (issueRequest.Quantity <= 0)
            {
                throw new BusinessRuleViolationException("Issue quantity must be greater than zero.");
            }

            if (issueRequest.Quantity > item.RemainingQuantity)
            {
                throw new BusinessRuleViolationException(
                    $"Cannot issue {issueRequest.Quantity} units of {item.Product.SKU}. " +
                    $"Remaining requested quantity is {item.RemainingQuantity}.");
            }

            if (issueRequest.Quantity > item.Product.CurrentStock)
            {
                throw new InsufficientStockException(
                    item.ProductId,
                    item.Product.CurrentStock,
                    issueRequest.Quantity);
            }

            var notes = TextNormalization.OptionalOrNull(issueRequest.Notes);
            var reason = notes == null
                ? $"Issued for work order {workOrder.OrderNumber}"
                : $"Issued for work order {workOrder.OrderNumber}: {notes}";

            issuePlan.Add((item, issueRequest, reason));
        }

        var userId = _currentUser.RequireUserId();
        var timestamp = DateTime.UtcNow;
        foreach (var (item, issueRequest, reason) in issuePlan)
        {
            var movement = StockMovement.Post(
                item.Product,
                request.OperationId,
                StockMovementType.Issue,
                issueRequest.Quantity,
                reason,
                workOrder.OrderNumber,
                userId,
                workOrder.Id,
                timestamp);

            item.Issue(issueRequest.Quantity);
            await _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);
        }

        try
        {
            _unitOfWork.WorkOrders.Update(workOrder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "Stock or work-order data changed concurrently. Refresh and retry with a new operation id.",
                exception);
        }

        return workOrder.ToDto();
    }

    private static bool IsReplayOf(
        IReadOnlyCollection<StockMovement> priorMovements,
        IssueWorkOrderItemsCommand request,
        WorkOrder workOrder)
    {
        if (priorMovements.Count != request.Items.Count
            || priorMovements.Any(movement =>
                movement.Type != StockMovementType.Issue
                || movement.WorkOrderId != request.WorkOrderId
                || !string.Equals(movement.Reference, workOrder.OrderNumber, StringComparison.Ordinal)))
        {
            return false;
        }

        var requestedByProduct = request.Items.ToDictionary(item => item.ProductId);
        foreach (var movement in priorMovements)
        {
            if (!requestedByProduct.TryGetValue(movement.ProductId, out var requested))
            {
                return false;
            }

            var notes = TextNormalization.OptionalOrNull(requested.Notes);
            var expectedReason = notes == null
                ? $"Issued for work order {workOrder.OrderNumber}"
                : $"Issued for work order {workOrder.OrderNumber}: {notes}";

            if (movement.Quantity != requested.Quantity
                || !string.Equals(movement.Reason, expectedReason, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
