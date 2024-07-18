using KimaiAutoEntry;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

using CliWrap;

const string ServiceName = "KimaiAutoEntry Service";

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = ServiceName;
});

if (OperatingSystem.IsWindows())
{
    LoggerProviderOptions.RegisterProviderOptions<
        EventLogSettings, EventLogLoggerProvider>(builder.Services);
}

builder.Services.AddSingleton<KimaiServer>();
builder.Services.AddHostedService<WindowsBackgroundService>();

if (args is { Length: 1 })
{
    try
    {
        string executablePath =
            Path.Combine(AppContext.BaseDirectory, "KimaiAutoEntry.exe");

        if (args[0] is "/Install")
        {
            await Cli.Wrap("sc.exe")
                .WithArguments(["create", ServiceName, $"binPath={executablePath}", "start=auto"])
                .WithArguments(["start", ServiceName])
                .ExecuteAsync();
        }
        else if (args[0] is "/Uninstall")
        {
            await Cli.Wrap("sc.exe")
                .WithArguments(["stop", ServiceName])
                .ExecuteAsync();

            await Cli.Wrap("sc.exe")
                .WithArguments(["delete", ServiceName])
                .ExecuteAsync();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }

    return;
}

IHost host = builder.Build();
host.Run();