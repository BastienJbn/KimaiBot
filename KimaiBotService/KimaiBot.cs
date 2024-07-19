namespace KimaiBotService;

using System.IO.Pipes;
using System.Net.Http;

public sealed class KimaiBot
{
    private readonly System.Timers.Timer timer = new();

    private readonly KimaiHttpClient httpClient = new();

    public KimaiBot()
    {
        
    }

    public string HandleCommand(string request)
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

                if (httpClient.Authenticate())
                {
                    // Start timer. Trigger each day at 10am
                    timer.Elapsed += (sender, e) => httpClient.AddEntryComboRnD();
                    timer.Interval = 1000 * 60 * 60 * 24;
                    timer.Start();

                    Console.WriteLine("Successfully logged in. User = " + username);
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

            default:
                Console.WriteLine("Invalid command.");
                return "Invalid command.";
        }
    }
}