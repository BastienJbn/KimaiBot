using KimaiBotService;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

using CliWrap;

const string ServiceName = "KimaiBot";

if (args is { Length: 1 })
{
    try
    {
        string executablePath =
            Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.exe");

        if (args[0] is "/Install")
        {
            var result = await Cli.Wrap("sc.exe")
                .WithArguments(["create", ServiceName, $"binPath={executablePath}", "start=auto"])
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync();

            result = await Cli.Wrap("sc.exe")
                .WithArguments(["start", ServiceName])
                .WithValidation(CommandResultValidation.None)
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

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = ServiceName;
});

LoggerProviderOptions.RegisterProviderOptions<
    EventLogSettings, EventLogLoggerProvider>(builder.Services);

builder.Services.AddSingleton<KimaiBot>();
builder.Services.AddHostedService<WindowsBackgroundService>();
IHost host = builder.Build();
host.Run();
