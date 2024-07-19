namespace KimaiBotService;

public sealed class WindowsBackgroundService(
    KimaiBot bot) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Run(() =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bot.Start();

                // Wait for the service to be stopped
                // Tasks launched by the bot will continue to run
                // until the service is stopped
            }
        }, stoppingToken);
    }
}
