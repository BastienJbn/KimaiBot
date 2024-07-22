namespace KimaiBotService;

public sealed class WindowsBackgroundService(
    KimaiBot bot) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bot.Start(stoppingToken);
    }
}
