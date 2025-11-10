using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.Audit;

/// <summary>
/// Handler for getting audit logs from multiple entity types
/// </summary>
public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var auditLogs = new List<AuditLogDto>();

        // Ensure ToDate includes the entire day (set to end of day)
        var toDate = request.ToDate.HasValue ? request.ToDate.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null;

        // Get audit logs from different entity types based on filter
        var shouldIncludeProducts = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "Product";
        var shouldIncludeWorkOrders = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "WorkOrder";
        var shouldIncludeUsers = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "User";
        var shouldIncludeStockMovements = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "StockMovement";

        // Products
        if (shouldIncludeProducts)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => string.IsNullOrEmpty(request.PerformedBy) ||
                            p.CreatedBy.Contains(request.PerformedBy) ||
                            (p.ModifiedBy != null && p.ModifiedBy.Contains(request.PerformedBy)) ||
                            (p.DeletedBy != null && p.DeletedBy.Contains(request.PerformedBy)))
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                // Created action
                if ((string.IsNullOrEmpty(request.Action) || request.Action == "Created") &&
                    (!request.FromDate.HasValue || product.CreatedAt >= request.FromDate.Value) &&
                    (!toDate.HasValue || product.CreatedAt <= toDate.Value))
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "Product",
                        EntityId = product.Id,
                        EntityIdentifier = $"{product.SKU} - {product.Name}",
                        Action = "Created",
                        Timestamp = product.CreatedAt,
                        PerformedBy = product.CreatedBy
                    });
                }

                // Modified action
                if (product.ModifiedAt.HasValue &&
                    (string.IsNullOrEmpty(request.Action) || request.Action == "Modified") &&
                    (!request.FromDate.HasValue || product.ModifiedAt.Value >= request.FromDate.Value) &&
                    (!toDate.HasValue || product.ModifiedAt.Value <= toDate.Value))
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "Product",
                        EntityId = product.Id,
                        EntityIdentifier = $"{product.SKU} - {product.Name}",
                        Action = "Modified",
                        Timestamp = product.ModifiedAt.Value,
                        PerformedBy = product.ModifiedBy ?? "System"
                    });
                }

                // Deleted action
                if (product.IsDeleted &&
                    (string.IsNullOrEmpty(request.Action) || request.Action == "Deleted"))
                {
                    var deletedTimestamp = product.DeletedAt ?? product.ModifiedAt ?? product.CreatedAt;
                    if ((!request.FromDate.HasValue || deletedTimestamp >= request.FromDate.Value) &&
                        (!toDate.HasValue || deletedTimestamp <= toDate.Value))
                    {
                        auditLogs.Add(new AuditLogDto
                        {
                            EntityType = "Product",
                            EntityId = product.Id,
                            EntityIdentifier = $"{product.SKU} - {product.Name}",
                            Action = "Deleted",
                            Timestamp = deletedTimestamp,
                            PerformedBy = product.DeletedBy ?? product.ModifiedBy ?? "System"
                        });
                    }
                }
            }
        }

        // Work Orders
        if (shouldIncludeWorkOrders)
        {
            var workOrders = await _context.WorkOrders
                .AsNoTracking()
                .Where(w => string.IsNullOrEmpty(request.PerformedBy) ||
                            w.CreatedBy.Contains(request.PerformedBy) ||
                            (w.ModifiedBy != null && w.ModifiedBy.Contains(request.PerformedBy)))
                .ToListAsync(cancellationToken);

            foreach (var workOrder in workOrders)
            {
                // Created action
                if ((string.IsNullOrEmpty(request.Action) || request.Action == "Created") &&
                    (!request.FromDate.HasValue || workOrder.CreatedAt >= request.FromDate.Value) &&
                    (!toDate.HasValue || workOrder.CreatedAt <= toDate.Value))
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "WorkOrder",
                        EntityId = workOrder.Id,
                        EntityIdentifier = $"{workOrder.OrderNumber} - {workOrder.Title}",
                        Action = "Created",
                        Timestamp = workOrder.CreatedAt,
                        PerformedBy = workOrder.CreatedBy
                    });
                }

                // Modified action
                if (workOrder.ModifiedAt.HasValue &&
                    (string.IsNullOrEmpty(request.Action) || request.Action == "Modified") &&
                    (!request.FromDate.HasValue || workOrder.ModifiedAt.Value >= request.FromDate.Value) &&
                    (!toDate.HasValue || workOrder.ModifiedAt.Value <= toDate.Value))
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "WorkOrder",
                        EntityId = workOrder.Id,
                        EntityIdentifier = $"{workOrder.OrderNumber} - {workOrder.Title}",
                        Action = "Modified",
                        Timestamp = workOrder.ModifiedAt.Value,
                        PerformedBy = workOrder.ModifiedBy ?? "System"
                    });
                }
            }
        }

        // Users
        if (shouldIncludeUsers)
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => string.IsNullOrEmpty(request.PerformedBy) ||
                            u.CreatedBy.Contains(request.PerformedBy) ||
                            (u.ModifiedBy != null && u.ModifiedBy.Contains(request.PerformedBy)))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                // Created action
                if ((string.IsNullOrEmpty(request.Action) || request.Action == "Created") &&
                    (!request.FromDate.HasValue || user.CreatedAt >= request.FromDate.Value) &&
                    (!toDate.HasValue || user.CreatedAt <= toDate.Value))
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "User",
                        EntityId = user.Id,
                        EntityIdentifier = $"{user.Email} - {user.FullName}",
                        Action = "Created",
                        Timestamp = user.CreatedAt,
                        PerformedBy = user.CreatedBy
                    });
                }

                // Modified action
                if (user.ModifiedAt.HasValue &&
                    (string.IsNullOrEmpty(request.Action) || request.Action == "Modified") &&
                    (!request.FromDate.HasValue || user.ModifiedAt.Value >= request.FromDate.Value) &&
                    (!toDate.HasValue || user.ModifiedAt.Value <= toDate.Value))
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "User",
                        EntityId = user.Id,
                        EntityIdentifier = $"{user.Email} - {user.FullName}",
                        Action = "Modified",
                        Timestamp = user.ModifiedAt.Value,
                        PerformedBy = user.ModifiedBy ?? "System"
                    });
                }
            }
        }

        // Stock Movements
        if (shouldIncludeStockMovements)
        {
            var movements = await _context.StockMovements
                .Include(s => s.Product)
                .AsNoTracking()
                .Where(s => !request.FromDate.HasValue || s.Timestamp >= request.FromDate.Value)
                .Where(s => !toDate.HasValue || s.Timestamp <= toDate.Value)
                .ToListAsync(cancellationToken);

            foreach (var movement in movements)
            {
                if (string.IsNullOrEmpty(request.Action) || request.Action == "Created")
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "StockMovement",
                        EntityId = movement.Id,
                        EntityIdentifier = $"{movement.Product?.SKU ?? "Unknown"} - {movement.Type} ({movement.Quantity})",
                        Action = "Created",
                        Timestamp = movement.Timestamp,
                        PerformedBy = "Stock System",
                        Details = movement.Reason
                    });
                }
            }
        }

        // Sort by timestamp descending and apply pagination
        var totalCount = auditLogs.Count;
        var paginatedLogs = auditLogs
            .OrderByDescending(a => a.Timestamp)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PaginatedResult<AuditLogDto>(
            paginatedLogs,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
