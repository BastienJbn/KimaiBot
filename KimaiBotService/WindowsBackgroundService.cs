using System.IO.Pipes;

namespace KimaiBotService;

public sealed class WindowsBackgroundService(
    KimaiBot server) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await server.HandlePipeServer();
        }
    }
}
