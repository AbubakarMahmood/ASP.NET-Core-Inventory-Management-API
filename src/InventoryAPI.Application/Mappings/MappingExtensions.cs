using InventoryAPI.Application.Commands.Products;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;

namespace InventoryAPI.Application.Mappings;

/// <summary>
/// Explicit entity-to-DTO mapping. Kept as plain extension methods so the
/// mappings are compile-time checked and trivially debuggable.
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
        CostingMethod = product.CostingMethod,
        IsLowStock = product.IsLowStock(),
        CreatedAt = product.CreatedAt,
        Version = product.Version
    };

    public static Product ToEntity(this CreateProductCommand command) => new()
    {
        SKU = command.SKU,
        Name = command.Name,
        Description = command.Description,
        Category = command.Category,
        CurrentStock = command.CurrentStock,
        ReorderPoint = command.ReorderPoint,
        ReorderQuantity = command.ReorderQuantity,
        UnitOfMeasure = command.UnitOfMeasure,
        UnitCost = command.UnitCost,
        Location = command.Location,
        CostingMethod = command.CostingMethod
    };

    /// <summary>
    /// Maps a stock movement. Product/PerformedBy/WorkOrder navigation
    /// properties populate the denormalized fields when they are loaded.
    /// </summary>
    public static StockMovementDto ToDto(this StockMovement movement) => new()
    {
        Id = movement.Id,
        ProductId = movement.ProductId,
        ProductSKU = movement.Product?.SKU ?? string.Empty,
        ProductName = movement.Product?.Name ?? string.Empty,
        Type = movement.Type,
        Quantity = movement.Quantity,
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

    /// <summary>
    /// Maps a work order with its items. RequestedBy/AssignedTo and each
    /// item's Product must be loaded for the related fields to populate.
    /// </summary>
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
