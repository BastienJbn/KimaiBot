using KimaiBotService;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using CliWrap;

const string ServiceName = "KimaiBot";

// Installation commands
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

// Configure the service to run as a Windows service
HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = ServiceName;
});

// Configure logging
builder.Services.AddLogging(options =>
{
    options.AddEventSourceLogger();
    options.SetMinimumLevel(LogLevel.Trace);
});

// Add KimaiBot as a hosted service
builder.Services.AddHostedService<KimaiBot>();

// Build and run the host
IHost host = builder.Build();
host.Run();
