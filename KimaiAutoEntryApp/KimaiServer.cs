using System.IO.Pipes;

namespace KimaiAutoEntry;

public sealed class KimaiServer
{
    private readonly NamedPipeServerStream pipeServer = 
        new("KimaiAutoEntryPipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    private readonly System.Timers.Timer timer = new System.Timers.Timer();

    private readonly HttpClient client;
    private readonly HttpClientHandler handler;

    private readonly string loginHttpAddress = "http://frapp01/kimai/index.php?a=checklogin";
    private readonly string processorHttpAddress = "http://frapp01/kimai/extensions/ki_timesheets/processor.php";
    private readonly string username = "";
    private readonly string password = "";

    private readonly static string helpString =
        "Available commands:\n" +
        "\tkimai login <username> <password>\n" +
        "\tkimai add <project> <activity> <duration> <comment>\n" +
        "\tkimai logout\n" +
        "\tkimai help\n";

    public KimaiServer()
    {
        handler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer()
        };

        client = new HttpClient(handler)
        {
            BaseAddress = new Uri(processorHttpAddress)
        };

        Console.WriteLine("Kimai server started. Waiting for login command.");
    }

    public string HandleCommand(string? request)
    {
        // the command is a console command. Several commands are available:
        // kimai login <username> <password>
        // kimai addEntry
        // kimai logout
        // kimai help

        if (request == null)
            return helpString;

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

                if (Authenticate())
                {
                    // Start timer. Trigger each day at 10am
                    timer.Elapsed += (sender, e) => AddEntryComboRnD();
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
                handler.CookieContainer = new System.Net.CookieContainer();
                Console.WriteLine("Successfully logged out.");
                return "Successfully logged out.";

            case "help":
                return helpString;

            default:
                Console.WriteLine("Invalid command.");
                return "Invalid command.";
        }
    }

    public async Task HandlePipeServer()
    {
        await pipeServer.WaitForConnectionAsync();

        using var reader = new StreamReader(pipeServer);
        using var writer = new StreamWriter(pipeServer);

        writer.AutoFlush = true;

        string? request = await reader.ReadLineAsync();

        string response = HandleCommand(request);

        await writer.WriteLineAsync(response);

        pipeServer.Disconnect();
    }

    public bool Authenticate()
    {
        var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("name", username),
            new KeyValuePair<string, string>("password", password)
        ]);

        var response = client.PostAsync(loginHttpAddress, content).Result;

        return response.IsSuccessStatusCode;
    }

    private bool AddEntryComboRnD()
    {
        var payload = new List<KeyValuePair<string, string>>
        {
            new("axAction",    "add_edit_timeSheetEntry"),
            new("projectID",   "18"),
            new("activityID",  "52"),
            new("description", ""),
            new("start_day",   DateTime.Now.ToString("dd.MM.yyyy")),
            new("end_day",     DateTime.Now.ToString("dd.MM.yyyy")),
            new("start_time",  "00:00:00"),
            new("end_time",    "07:00:00"),
            new("duration",    "07:00:00"),
            new("comment",     ""),
            new("commentType", "0"),
            new("userID[]",    "675906454"),
            new("statusID",    "1"),
            new("billable",    "0")
        };
    
        var content = new FormUrlEncodedContent(payload);
        var response = client.PostAsync(processorHttpAddress, content).Result;
    
        return response.IsSuccessStatusCode;
    }
}