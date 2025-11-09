using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.Audit;

/// <summary>
/// Query to get audit logs with filtering
/// </summary>
public class GetAuditLogsQuery : IRequest<PaginatedResult<AuditLogDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? EntityType { get; set; } // Product, WorkOrder, User, StockMovement
    public string? Action { get; set; } // Created, Modified, Deleted
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? PerformedBy { get; set; }
}
