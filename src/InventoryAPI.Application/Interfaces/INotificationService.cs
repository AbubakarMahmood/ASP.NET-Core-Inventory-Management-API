namespace InventoryAPI.Application.Interfaces;

/// <summary>
/// Service for sending real-time notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Send notification to all connected clients
    /// </summary>
    Task SendNotificationToAllAsync(string message, string type = "info");

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    Task SendNotificationToUserAsync(string userId, string message, string type = "info");

    /// <summary>
    /// Send work order notification
    /// </summary>
    Task SendWorkOrderNotificationAsync(string orderNumber, string action, string message);

    /// <summary>
    /// Send low stock notification
    /// </summary>
    Task SendLowStockNotificationAsync(string productSku, string productName, int currentStock);

    /// <summary>
    /// Send stock movement notification
    /// </summary>
    Task SendStockMovementNotificationAsync(string productSku, string movementType, int quantity);
}
