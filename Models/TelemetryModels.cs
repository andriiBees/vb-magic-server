using System.Text.Json.Serialization;

namespace MagicServer.Models;

public class BoardConfig
{
    public string Ip { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class BoardTelemetry
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public string State { get; set; } = "OK";
    public double Uptime { get; set; }
}

public class Vehicle
{
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public string State { get; set; } = "Offline";
    public double Uptime { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Altitude { get; set; }
    public double Speed { get; set; }
    public double Heading { get; set; }
    
    [JsonIgnore]
    public DateTime LastUpdateTime { get; set; }
}

public class MapData
{
    public List<Vehicle> Vehicles { get; set; } = new();
    public List<object> Waypoints { get; set; } = new();
    public List<object> Points { get; set; } = new();
    public List<object> Zones { get; set; } = new();
}