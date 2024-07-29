using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
public sealed class WindowsBackgroundService(
    ILogger<KimaiBot> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        KimaiBot bot = new(logger);
        await bot.Start(stoppingToken);
    }
}
