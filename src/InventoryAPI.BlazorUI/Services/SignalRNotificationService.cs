using Blazored.LocalStorage;
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

    public SignalRNotificationService(
        IConfiguration configuration,
        ILocalStorageService localStorage,
        ISnackbar snackbar,
        ILogger<SignalRNotificationService> logger)
    {
        _snackbar = snackbar;
        _logger = logger;

        var apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5000";
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{apiBaseUrl}/api/v1/notifications", options =>
            {
                // The hub requires authentication; browsers cannot set headers
                // on websocket requests, so the token travels as access_token.
                options.AccessTokenProvider = async () =>
                    await localStorage.GetItemAsync<string>("authToken");
            })
            .WithAutomaticReconnect()
            .Build();

        ConfigureHandlers();
    }

    private void ConfigureHandlers()
    {
        _hubConnection.On<GeneralNotification>("ReceiveNotification", notification =>
        {
            var severity = notification.Type switch
            {
                "success" => Severity.Success,
                "error" => Severity.Error,
                "warning" => Severity.Warning,
                _ => Severity.Info
            };

            _snackbar.Add(notification.Message ?? "New notification", severity);
        });

        _hubConnection.On<WorkOrderNotification>("ReceiveWorkOrderNotification", notification =>
        {
            _snackbar.Add(
                notification.Message ?? $"Work order {notification.OrderNumber} updated",
                Severity.Info);
        });

        _hubConnection.On<LowStockNotification>("ReceiveLowStockNotification", notification =>
        {
            _snackbar.Add(notification.Message ?? "Low stock alert", Severity.Warning);
        });

        _hubConnection.On<StockMovementNotification>("ReceiveStockMovementNotification", notification =>
        {
            _snackbar.Add(notification.Message ?? "Stock movement recorded", Severity.Info);
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

    private sealed record GeneralNotification(string? Message, string? Type, DateTime Timestamp);
    private sealed record WorkOrderNotification(string? OrderNumber, string? Action, string? Message, DateTime Timestamp);
    private sealed record LowStockNotification(string? ProductSku, string? ProductName, int CurrentStock, string? Message, DateTime Timestamp);
    private sealed record StockMovementNotification(string? ProductSku, string? MovementType, int Quantity, string? Message, DateTime Timestamp);
}
