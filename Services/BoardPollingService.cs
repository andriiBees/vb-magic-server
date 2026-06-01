using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using MagicServer.Hubs;
using MagicServer.Models;
using MagicServer.Utils;

namespace MagicServer.Services;

public class BoardPollingService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHubContext<MapHub> _hubContext;
    private readonly ILogger<BoardPollingService> _logger;
    private readonly Dictionary<string, Vehicle> _vehiclesState = new();

    public BoardPollingService(IHttpClientFactory httpClientFactory, IHubContext<MapHub> hubContext, ILogger<BoardPollingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Початок опитування бортів...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var mapData = await GatherDataAsync(stoppingToken);

                if (mapData.Vehicles.Count > 0)
                {
                    await _hubContext.Clients.All.SendAsync("RedrawMap", mapData, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка в циклі опитування.");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task<MapData> GatherDataAsync(CancellationToken stoppingToken)
    {
        var mapData = new MapData();
        var configPath = Path.Combine(AppContext.BaseDirectory, "boards.json");

        if (!File.Exists(configPath))
        {
            _logger.LogWarning("Файл boards.json не знайдено!");
            return mapData;
        }
        
        var configJson = await File.ReadAllTextAsync(configPath, stoppingToken);
        var boards = JsonSerializer.Deserialize<List<BoardConfig>>(configJson) ?? new List<BoardConfig>();

        var httpClient = _httpClientFactory.CreateClient("BoardClient");
        httpClient.Timeout = TimeSpan.FromSeconds(1);

        var tasks = boards.Select(async board =>
        {
            try
            {
                var url = $"http://{board.Ip}:5500/";
                var response = await httpClient.GetAsync(url, stoppingToken);
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(stoppingToken);
                    var telemetry = JsonSerializer.Deserialize<BoardTelemetry>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return telemetry != null ? UpdateVehicleState(board, telemetry) : GetOfflineVehicle(board);
                }
                
                return GetOfflineVehicle(board);
            }
            catch
            {
                return GetOfflineVehicle(board);
            }
        });

        var results = await Task.WhenAll(tasks);
        mapData.Vehicles.AddRange(results);
        
        return mapData;
    }

    private Vehicle UpdateVehicleState(BoardConfig board, BoardTelemetry telemetry)
    {
        var now = DateTime.UtcNow;
        if (!_vehiclesState.TryGetValue(board.Ip, out var previousState))
        {
            previousState = new Vehicle { Ip = board.Ip, Name = board.Name };
        }

        double speed = 0;
        double heading = previousState.Heading;

        if (previousState.LastUpdateTime != default)
        {
            var timeDiffSeconds = (now - previousState.LastUpdateTime).TotalSeconds;
            if (timeDiffSeconds > 0)
            {
                var distanceMeters = GeoMath.CalculateDistance(previousState.Latitude, previousState.Longitude, telemetry.Latitude, telemetry.Longitude);
                speed = distanceMeters / timeDiffSeconds;
                
                if (distanceMeters > 0.5) 
                {
                    heading = GeoMath.CalculateHeading(previousState.Latitude, previousState.Longitude, telemetry.Latitude, telemetry.Longitude);
                }
            }
        }

        var updatedVehicle = new Vehicle
        {
            Name = board.Name,
            Ip = board.Ip,
            State = telemetry.State,
            Uptime = telemetry.Uptime,
            Latitude = telemetry.Latitude,
            Longitude = telemetry.Longitude,
            Altitude = telemetry.Altitude,
            Speed = Math.Round(speed, 2),
            Heading = Math.Round(heading, 2),
            LastUpdateTime = now
        };

        _vehiclesState[board.Ip] = updatedVehicle;
        return updatedVehicle;
    }

    private Vehicle GetOfflineVehicle(BoardConfig board)
    {
        if (_vehiclesState.TryGetValue(board.Ip, out var lastState))
        {
            lastState.State = "Offline";
            lastState.Speed = 0;
            lastState.LastUpdateTime = DateTime.UtcNow;
            return lastState;
        }

        var offlineVehicle = new Vehicle { Name = board.Name, Ip = board.Ip, State = "Offline", LastUpdateTime = DateTime.UtcNow };
        _vehiclesState[board.Ip] = offlineVehicle;
        return offlineVehicle;
    }
}