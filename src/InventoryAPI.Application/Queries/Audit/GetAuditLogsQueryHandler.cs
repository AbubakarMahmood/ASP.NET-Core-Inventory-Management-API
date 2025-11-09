using InventoryAPI.Application.DTOs;
using InventoryAPI.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.Audit;

/// <summary>
/// Handler for getting audit logs from multiple entity types
/// </summary>
public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    private readonly ApplicationDbContext _context;

    public GetAuditLogsQueryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var auditLogs = new List<AuditLogDto>();

        // Get audit logs from different entity types based on filter
        var shouldIncludeProducts = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "Product";
        var shouldIncludeWorkOrders = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "WorkOrder";
        var shouldIncludeUsers = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "User";
        var shouldIncludeStockMovements = string.IsNullOrEmpty(request.EntityType) || request.EntityType == "StockMovement";

        // Products
        if (shouldIncludeProducts)
        {
            var products = await _context.Products
                .Where(p => !request.FromDate.HasValue || p.CreatedAt >= request.FromDate.Value)
                .Where(p => !request.ToDate.HasValue || p.CreatedAt <= request.ToDate.Value)
                .Where(p => string.IsNullOrEmpty(request.PerformedBy) || p.CreatedBy.Contains(request.PerformedBy))
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                if (string.IsNullOrEmpty(request.Action) || request.Action == "Created")
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

                if (product.ModifiedAt.HasValue && (string.IsNullOrEmpty(request.Action) || request.Action == "Modified"))
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

                if (product.IsDeleted && (string.IsNullOrEmpty(request.Action) || request.Action == "Deleted"))
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "Product",
                        EntityId = product.Id,
                        EntityIdentifier = $"{product.SKU} - {product.Name}",
                        Action = "Deleted",
                        Timestamp = product.DeletedAt ?? product.ModifiedAt ?? product.CreatedAt,
                        PerformedBy = product.DeletedBy ?? product.ModifiedBy ?? "System"
                    });
                }
            }
        }

        // Work Orders
        if (shouldIncludeWorkOrders)
        {
            var workOrders = await _context.WorkOrders
                .Where(w => !request.FromDate.HasValue || w.CreatedAt >= request.FromDate.Value)
                .Where(w => !request.ToDate.HasValue || w.CreatedAt <= request.ToDate.Value)
                .Where(w => string.IsNullOrEmpty(request.PerformedBy) || w.CreatedBy.Contains(request.PerformedBy))
                .ToListAsync(cancellationToken);

            foreach (var workOrder in workOrders)
            {
                if (string.IsNullOrEmpty(request.Action) || request.Action == "Created")
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

                if (workOrder.ModifiedAt.HasValue && (string.IsNullOrEmpty(request.Action) || request.Action == "Modified"))
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
                .Where(u => !request.FromDate.HasValue || u.CreatedAt >= request.FromDate.Value)
                .Where(u => !request.ToDate.HasValue || u.CreatedAt <= request.ToDate.Value)
                .Where(u => string.IsNullOrEmpty(request.PerformedBy) || u.CreatedBy.Contains(request.PerformedBy))
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                if (string.IsNullOrEmpty(request.Action) || request.Action == "Created")
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

                if (user.ModifiedAt.HasValue && (string.IsNullOrEmpty(request.Action) || request.Action == "Modified"))
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
                .Where(s => !request.FromDate.HasValue || s.Timestamp >= request.FromDate.Value)
                .Where(s => !request.ToDate.HasValue || s.Timestamp <= request.ToDate.Value)
                .ToListAsync(cancellationToken);

            foreach (var movement in movements)
            {
                if (string.IsNullOrEmpty(request.Action) || request.Action == "Created")
                {
                    auditLogs.Add(new AuditLogDto
                    {
                        EntityType = "StockMovement",
                        EntityId = movement.Id,
                        EntityIdentifier = $"{movement.Product?.SKU} - {movement.Type} ({movement.Quantity})",
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
