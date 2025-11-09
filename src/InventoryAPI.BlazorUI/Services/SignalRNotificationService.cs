using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;

namespace InventoryAPI.BlazorUI.Services;

/// <summary>
/// Service for receiving real-time notifications via SignalR
/// </summary>
public class SignalRNotificationService : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ISnackbar _snackbar;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(IConfiguration configuration, ISnackbar snackbar, ILogger<SignalRNotificationService> logger)
    {
        _snackbar = snackbar;
        _logger = logger;

        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/api/v1/notifications")
            .WithAutomaticReconnect()
            .Build();

        ConfigureHandlers();
    }

    private void ConfigureHandlers()
    {
        _hubConnection.On<dynamic>("ReceiveNotification", (notification) =>
        {
            var message = notification.message?.ToString() ?? "New notification";
            var type = notification.type?.ToString() ?? "info";

            var severity = type switch
            {
                "success" => Severity.Success,
                "error" => Severity.Error,
                "warning" => Severity.Warning,
                _ => Severity.Info
            };

            _snackbar.Add(message, severity);
        });

        _hubConnection.On<dynamic>("ReceiveWorkOrderNotification", (notification) =>
        {
            var orderNumber = notification.orderNumber?.ToString() ?? "";
            var message = notification.message?.ToString() ?? $"Work order {orderNumber} updated";
            _snackbar.Add(message, Severity.Info);
        });

        _hubConnection.On<dynamic>("ReceiveLowStockNotification", (notification) =>
        {
            var message = notification.message?.ToString() ?? "Low stock alert";
            _snackbar.Add(message, Severity.Warning);
        });

        _hubConnection.On<dynamic>("ReceiveStockMovementNotification", (notification) =>
        {
            var message = notification.message?.ToString() ?? "Stock movement recorded";
            _snackbar.Add(message, Severity.Info);
        });
    }

    public async Task StartAsync()
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Disconnected)
            {
                await _hubConnection.StartAsync();
                _logger.LogInformation("SignalR connection started");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting SignalR connection");
        }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.StopAsync();
                _logger.LogInformation("SignalR connection stopped");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping SignalR connection");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
