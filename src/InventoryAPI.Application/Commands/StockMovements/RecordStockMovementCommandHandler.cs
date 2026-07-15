using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.StockMovements;

public class RecordStockMovementCommandHandler : IRequestHandler<RecordStockMovementCommand, StockMovementDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RecordStockMovementCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<StockMovementDto> Handle(RecordStockMovementCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var userId = _currentUser.RequireUserId();

        if (request.WorkOrderId.HasValue)
        {
            var workOrderExists = await _unitOfWork.WorkOrders
                .AnyAsync(w => w.Id == request.WorkOrderId.Value, cancellationToken);
            if (!workOrderExists)
                throw new NotFoundException(nameof(WorkOrder), request.WorkOrderId.Value);
        }

        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            Type = request.Type,
            Quantity = request.Quantity,
            SourceLocation = request.SourceLocation,
            DestinationLocation = request.DestinationLocation,
            Reason = request.Reason,
            Reference = request.Reference,
            WorkOrderId = request.WorkOrderId,
            PerformedById = userId,
            Timestamp = DateTime.UtcNow,
            UnitCostAtTransaction = product.UnitCost
        };

        ApplyToStock(product, request);

        // One SaveChanges commits the movement and the stock change atomically.
        await _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);
        _unitOfWork.Products.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var result = movement.ToDto();
        result.ProductSKU = product.SKU;
        result.ProductName = product.Name;

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            result.PerformedByName = user.FullName;
        }

        return result;
    }

    private static void ApplyToStock(Product product, RecordStockMovementCommand request)
    {
        switch (request.Type)
        {
            case StockMovementType.Receipt:
            case StockMovementType.Return:
                product.AdjustStock(request.Quantity);
                break;

            case StockMovementType.Issue:
                product.AdjustStock(-request.Quantity);
                break;

            case StockMovementType.Adjustment:
                // Adjustments carry their own sign: positive increases stock,
                // negative decreases it.
                product.AdjustStock(request.Quantity);
                break;

            case StockMovementType.Transfer:
                // Transfers relocate stock without changing the quantity on hand.
                if (!string.IsNullOrEmpty(request.DestinationLocation))
                {
                    product.Location = request.DestinationLocation;
                }
                break;
        }
    }
}
