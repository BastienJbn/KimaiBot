using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
public sealed class KimaiBot
{
    private readonly System.Timers.Timer timer = new();

    private readonly KimaiHttpClient httpClient = new();

    private readonly PipeServer server = new();

    public async Task Start(CancellationToken token)
    {
        await Task.WhenAll(CommandHandler(token), server.Start(token));
    }

    private Task CommandHandler(CancellationToken token)
    {
        return Task.Run(() =>
        {
            while (true)
            {
                string request = server.GetRequest();

                if(request == string.Empty) {
                    continue;
                }

                string response = HandleCommand(request);
                server.SendResponse(response);
            }
        }, token);
    }

    private string HandleCommand(string request)
    {
        string[] args = request.Split(' ');

        if (args.Length == 0)
        {
            Console.WriteLine("Received empty command.");
            return "No command provided.";
        }

        switch (args[0])
        {
            case "login":
                string username = args[1];
                string password = args[2];

                if (httpClient.Authenticate(username, password))
                {
                    // Add entry immediately
                    httpClient.AddEntryComboRnD();

                    // Start timer. Trigger each day at 10am
                    timer.Elapsed += (sender, e) => httpClient.AddEntryComboRnD();
                    timer.Interval = 1000 * 60 * 60 * 24;
                    timer.Start();

                    Console.WriteLine("Successfully logged in.");
                    return "Successfully logged in.";
                }
                else
                {
                    Console.WriteLine("Failed to log in.");
                    return "Failed to log in.";
                }

            case "logout":
                httpClient.Logout();
                timer.Stop();
                Console.WriteLine("Logged out.");
                return "Successfully logged out.";

            case "addEntry":
                httpClient.AddEntryComboRnD();
                Console.WriteLine("Added entry.");
                return "Successfully added entry.";

            default:
                Console.WriteLine("Invalid command.");
                return "Invalid command.";
        }
    }
}