using System.IO.Pipes;

namespace KimaiAutoEntry;

public sealed class WindowsBackgroundService(
    KimaiServer server) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await server.HandlePipeServer();
        }
    }
}
