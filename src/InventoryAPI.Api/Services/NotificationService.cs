using InventoryAPI.Api.Hubs;
using InventoryAPI.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace InventoryAPI.Api.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<NotificationHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationToAllAsync(string message, string type = "info")
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
            {
                message,
                type,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Notification sent to all clients: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to all clients");
        }
    }

    public async Task SendNotificationToUserAsync(string userId, string message, string type = "info")
    {
        try
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                message,
                type,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Notification sent to user {UserId}: {Message}", userId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }

    public async Task SendWorkOrderNotificationAsync(string orderNumber, string action, string message)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveWorkOrderNotification", new
            {
                orderNumber,
                action,
                message,
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Work order notification sent: {OrderNumber} - {Action}", orderNumber, action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending work order notification");
        }
    }

    public async Task SendLowStockNotificationAsync(string productSku, string productName, int currentStock)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveLowStockNotification", new
            {
                productSku,
                productName,
                currentStock,
                message = $"Low stock alert: {productName} ({productSku}) - Only {currentStock} remaining",
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Low stock notification sent: {ProductSku}", productSku);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending low stock notification");
        }
    }

    public async Task SendStockMovementNotificationAsync(string productSku, string movementType, int quantity)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveStockMovementNotification", new
            {
                productSku,
                movementType,
                quantity,
                message = $"Stock {movementType}: {productSku} - Quantity: {quantity}",
                timestamp = DateTime.UtcNow
            });

            _logger.LogInformation("Stock movement notification sent: {ProductSku} - {MovementType}", productSku, movementType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending stock movement notification");
        }
    }
}
