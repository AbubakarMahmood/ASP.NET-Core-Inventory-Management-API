using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.WorkOrders;

/// <summary>
/// Handler for creating a new work order
/// </summary>
public class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, WorkOrderDto>
{
    private const int OrderNumberRetries = 3;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateWorkOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<WorkOrderDto> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.RequireUserId();

        // Validate products exist before creating anything
        foreach (var item in request.Items)
        {
            var productExists = await _unitOfWork.Products
                .AnyAsync(p => p.Id == item.ProductId, cancellationToken);

            if (!productExists)
            {
                throw new NotFoundException(nameof(Product), item.ProductId);
            }
        }

        var workOrder = new WorkOrder
        {
            OrderNumber = await GenerateOrderNumberAsync(cancellationToken),
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            RequestedById = currentUserId,
            Status = WorkOrderStatus.Draft
        };

        foreach (var itemRequest in request.Items)
        {
            workOrder.Items.Add(new WorkOrderItem
            {
                ProductId = itemRequest.ProductId,
                QuantityRequested = itemRequest.QuantityRequested,
                QuantityIssued = 0,
                Notes = itemRequest.Notes
            });
        }

        await _unitOfWork.WorkOrders.AddAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties for a fully-populated DTO
        var savedWorkOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(workOrder.Id, cancellationToken);

        return savedWorkOrder!.ToDto();
    }

    /// <summary>
    /// Generates a daily-sequential order number (WO-yyyyMMdd-0001). The unique
    /// index on OrderNumber is the real guarantee; this probes for a free slot
    /// so collisions under concurrency surface as a clear error, not silently.
    /// </summary>
    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var prefix = $"WO-{today:yyyyMMdd}-";

        var todayCount = await _unitOfWork.WorkOrders
            .CountAsync(w => w.OrderNumber.StartsWith(prefix), cancellationToken);

        for (var attempt = 0; attempt < OrderNumberRetries; attempt++)
        {
            var candidate = $"{prefix}{todayCount + 1 + attempt:D4}";
            var taken = await _unitOfWork.WorkOrders
                .AnyAsync(w => w.OrderNumber == candidate, cancellationToken);

            if (!taken)
            {
                return candidate;
            }
        }

        throw new BusinessRuleViolationException(
            "Could not allocate a unique work order number. Please retry.");
    }
}
