using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.WorkOrders;

/// <summary>
/// Handler for submitting a work order
/// </summary>
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
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId);

        workOrder.Submit();

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Submitted",
            $"Work order {workOrder.OrderNumber} has been submitted for approval");

        return workOrder.ToDto();
    }
}

/// <summary>
/// Handler for approving a work order
/// </summary>
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
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId);

        var assignedUser = await _unitOfWork.Users.GetByIdAsync(request.AssignedToId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.AssignedToId);

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

/// <summary>
/// Handler for rejecting a work order
/// </summary>
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
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId);

        workOrder.Reject(request.Reason);

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Rejected",
            $"Work order {workOrder.OrderNumber} has been rejected: {workOrder.RejectionReason}");

        return workOrder.ToDto();
    }
}

/// <summary>
/// Handler for starting a work order
/// </summary>
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
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId);

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

/// <summary>
/// Handler for completing a work order
/// </summary>
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
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId);

        workOrder.Complete();

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.SendWorkOrderNotificationAsync(
            workOrder.OrderNumber,
            "Completed",
            $"Work order {workOrder.OrderNumber} has been completed");

        return workOrder.ToDto();
    }
}

/// <summary>
/// Handler for cancelling a work order
/// </summary>
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
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId);

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
/// Handler for issuing items from a work order. Creates the stock movements,
/// decrements product stock, and updates issued quantities in one atomic save.
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

    public async Task<WorkOrderDto> Handle(IssueWorkOrderItemsCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId);

        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            throw new BusinessRuleViolationException("Only in-progress work orders can have items issued.");
        }

        var userId = _currentUser.RequireUserId();

        foreach (var issueRequest in request.Items)
        {
            var item = workOrder.Items.FirstOrDefault(i => i.ProductId == issueRequest.ProductId)
                ?? throw new NotFoundException(
                    $"Product {issueRequest.ProductId} is not part of work order {workOrder.OrderNumber}.");

            if (issueRequest.Quantity <= 0)
            {
                throw new BusinessRuleViolationException("Issue quantity must be greater than zero.");
            }

            var remaining = item.QuantityRequested - item.QuantityIssued;
            if (issueRequest.Quantity > remaining)
            {
                throw new BusinessRuleViolationException(
                    $"Cannot issue {issueRequest.Quantity} units of {item.Product.SKU}. Remaining: {remaining}.");
            }

            // Throws InsufficientStockException when stock would go negative.
            item.Product.AdjustStock(-issueRequest.Quantity);
            item.QuantityIssued += issueRequest.Quantity;

            await _unitOfWork.StockMovements.AddAsync(new StockMovement
            {
                ProductId = item.ProductId,
                Type = StockMovementType.Issue,
                Quantity = issueRequest.Quantity,
                SourceLocation = issueRequest.FromLocation ?? item.Product.Location,
                Reason = $"Issued for work order {workOrder.OrderNumber}",
                Reference = workOrder.OrderNumber,
                WorkOrderId = workOrder.Id,
                PerformedById = userId,
                Timestamp = DateTime.UtcNow,
                UnitCostAtTransaction = item.Product.UnitCost
            }, cancellationToken);
        }

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return workOrder.ToDto();
    }
}
