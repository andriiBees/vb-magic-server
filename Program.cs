using MagicServer.Hubs;
using MagicServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5500); 
});

builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddHostedService<BoardPollingService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .SetIsOriginAllowed(_ => true) 
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapHub<MapHub>("/maphub");

app.MapGet("/", () => "MagicServer is running. Connect via SignalR at /maphub");

app.Run();
