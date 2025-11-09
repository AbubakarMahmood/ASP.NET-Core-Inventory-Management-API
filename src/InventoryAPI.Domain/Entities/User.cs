using InventoryAPI.Domain.Common;
using InventoryAPI.Domain.Enums;

namespace InventoryAPI.Domain.Entities;

/// <summary>
/// User entity for authentication and authorization
/// </summary>
public class User : BaseAuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Operator;
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // Navigation properties
    public ICollection<WorkOrder> RequestedWorkOrders { get; set; } = new List<WorkOrder>();
    public ICollection<WorkOrder> AssignedWorkOrders { get; set; } = new List<WorkOrder>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public string FullName => $"{FirstName} {LastName}";
}
