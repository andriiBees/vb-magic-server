using Microsoft.AspNetCore.SignalR;

namespace MagicServer.Hubs;

public class MapHub : Hub
{
    private readonly ILogger<MapHub> _logger;

    public MapHub(ILogger<MapHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Клієнт підключився до мапи: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Клієнт відключився від мапи: {Context.ConnectionId} " + 
                               (exception != null ? $"(Помилка: {exception.Message})" : ""));
        await base.OnDisconnectedAsync(exception);
    }
}