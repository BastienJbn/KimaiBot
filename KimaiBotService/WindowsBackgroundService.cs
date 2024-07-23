using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
public sealed class WindowsBackgroundService(
    KimaiBot bot) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bot.Start(stoppingToken);
    }
}
