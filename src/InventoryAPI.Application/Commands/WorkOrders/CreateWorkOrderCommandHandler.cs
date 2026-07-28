using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Mappings;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using MediatR;

namespace InventoryAPI.Application.Commands.WorkOrders;

public class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateWorkOrderCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<WorkOrderDto> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUser.RequireUserId();
        var duplicateProduct = request.Items
            .GroupBy(item => item.ProductId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateProduct != null)
        {
            throw new ValidationException(
                "Items",
                $"Product {duplicateProduct.Key} may appear only once on a work order.");
        }

        foreach (var item in request.Items)
        {
            var productExists = await _unitOfWork.Products
                .AnyAsync(product => product.Id == item.ProductId, cancellationToken);

            if (!productExists)
            {
                throw new NotFoundException(nameof(Product), item.ProductId);
            }
        }

        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var workOrder = new WorkOrder
        {
            OrderNumber = $"WO-{DateTime.UtcNow:yyyyMMdd}-{suffix}",
            Title = TextNormalization.Required(request.Title),
            Description = TextNormalization.Optional(request.Description),
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
                Notes = TextNormalization.OptionalOrNull(itemRequest.Notes)
            });
        }

        await _unitOfWork.WorkOrders.AddAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedWorkOrder = await _unitOfWork.WorkOrders
            .GetByIdWithDetailsAsync(workOrder.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), workOrder.Id);

        return savedWorkOrder.ToDto();
    }
}
