using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;

namespace InventoryAPI.Application.Mappings;

/// <summary>
/// Explicit, compile-time-checked entity-to-DTO mappings.
/// </summary>
public static class MappingExtensions
{
    public static ProductDto ToDto(this Product product) => new()
    {
        Id = product.Id,
        SKU = product.SKU,
        Name = product.Name,
        Description = product.Description,
        Category = product.Category,
        CurrentStock = product.CurrentStock,
        ReorderPoint = product.ReorderPoint,
        ReorderQuantity = product.ReorderQuantity,
        UnitOfMeasure = product.UnitOfMeasure,
        UnitCost = product.UnitCost,
        Location = product.Location,
        IsLowStock = product.IsLowStock(),
        CreatedAt = product.CreatedAt,
        Version = product.Version
    };

    public static StockMovementDto ToDto(this StockMovement movement) => new()
    {
        Id = movement.Id,
        OperationId = movement.OperationId,
        ProductId = movement.ProductId,
        ProductSKU = movement.Product?.SKU ?? string.Empty,
        ProductName = movement.Product?.Name ?? string.Empty,
        UnitOfMeasure = movement.Product?.UnitOfMeasure ?? string.Empty,
        Type = movement.Type,
        Quantity = movement.Quantity,
        QuantityDelta = StockMovement.CalculateQuantityDelta(movement.Type, movement.Quantity),
        BalanceBefore = movement.BalanceBefore,
        BalanceAfter = movement.BalanceAfter,
        SourceLocation = movement.SourceLocation,
        DestinationLocation = movement.DestinationLocation,
        Reason = movement.Reason,
        Reference = movement.Reference,
        WorkOrderId = movement.WorkOrderId,
        WorkOrderNumber = movement.WorkOrder?.OrderNumber,
        PerformedById = movement.PerformedById,
        PerformedByName = movement.PerformedBy?.FullName ?? string.Empty,
        Timestamp = movement.Timestamp,
        UnitCostAtTransaction = movement.UnitCostAtTransaction
    };

    public static WorkOrderDto ToDto(this WorkOrder workOrder) => new()
    {
        Id = workOrder.Id,
        OrderNumber = workOrder.OrderNumber,
        Priority = workOrder.Priority,
        Status = workOrder.Status,
        Title = workOrder.Title,
        Description = workOrder.Description,
        DueDate = workOrder.DueDate,
        CompletedDate = workOrder.CompletedDate,
        RejectionReason = workOrder.RejectionReason,
        IsFullyIssued = workOrder.IsFullyIssued,
        RequestedById = workOrder.RequestedById,
        RequestedByName = workOrder.RequestedBy?.FullName ?? string.Empty,
        RequestedByEmail = workOrder.RequestedBy?.Email ?? string.Empty,
        AssignedToId = workOrder.AssignedToId,
        AssignedToName = workOrder.AssignedTo?.FullName,
        AssignedToEmail = workOrder.AssignedTo?.Email,
        Items = workOrder.Items.Select(item => item.ToDto()).ToList(),
        CreatedAt = workOrder.CreatedAt,
        CreatedBy = workOrder.CreatedBy,
        ModifiedAt = workOrder.ModifiedAt,
        ModifiedBy = workOrder.ModifiedBy
    };

    public static WorkOrderItemDto ToDto(this WorkOrderItem item) => new()
    {
        Id = item.Id,
        WorkOrderId = item.WorkOrderId,
        ProductId = item.ProductId,
        ProductSKU = item.Product?.SKU ?? string.Empty,
        ProductName = item.Product?.Name ?? string.Empty,
        UnitOfMeasure = item.Product?.UnitOfMeasure ?? string.Empty,
        CurrentStock = item.Product?.CurrentStock ?? 0,
        QuantityRequested = item.QuantityRequested,
        QuantityIssued = item.QuantityIssued,
        RemainingQuantity = item.RemainingQuantity,
        IsFullyIssued = item.IsFullyIssued,
        Notes = item.Notes
    };

    public static UserDto ToDto(this User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        ModifiedAt = user.ModifiedAt,
        ModifiedBy = user.ModifiedBy
    };

    public static FilterPresetDto ToDto(this FilterPreset preset) => new()
    {
        Id = preset.Id,
        UserId = preset.UserId,
        Name = preset.Name,
        EntityType = preset.EntityType,
        FilterData = preset.FilterData,
        IsDefault = preset.IsDefault,
        IsShared = preset.IsShared,
        CreatedAt = preset.CreatedAt,
        CreatedBy = preset.CreatedBy,
        ModifiedAt = preset.ModifiedAt,
        ModifiedBy = preset.ModifiedBy
    };
}
