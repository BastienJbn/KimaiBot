namespace KimaiAutoEntry;

public sealed class WindowsBackgroundService(
    KimaiService KimaiService,
    ILogger<WindowsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool once = true;
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Authenticate once
            if(once)
            {
                if(KimaiService.Authenticate())
                    logger.LogInformation("Authentication successful");
                else
                    logger.LogError("Authentication failed");
                once = false;
            }

            if(KimaiService.AddEntryComboRnD())
                logger.LogInformation("Entry added successfully");
            else
                logger.LogError("Entry failed to add");

            // Wait for 1 day
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
