using KimaiBotCmdLine;

class Program
{
    // Main function when debugging
    #if DEBUG
    static void Main()
    {
        PipeClient client = new();
        Parser parser = new(client);

        if (!client.Connect())
        {
            Console.WriteLine("Failed to connect to the server!");
            return;
        }

        Console.WriteLine("Start ! Type 'exit' to quit.");

        while (true)
        {
            // Wait for user input
            Console.Write(">");
            var command = Console.ReadLine();
            Console.WriteLine(command);

            if (command == "exit")
                break;

            // Parse Command and process it
            string result = parser.HandleCommand(command);

            // Display result to user
            Console.WriteLine(result);
        }

        client.Disconnect();
    }
    #else

    // Main function when running the application
    static void Main(string[] args)
    {
        PipeClient client = new();
        Parser parser = new(client);

        if(args.Length == 0)
        {
            Console.WriteLine("No command provided.");
            Environment.Exit(1);
        }

        if (!client.Connect())
        {
            Console.WriteLine("Failed to connect to the server!");
            Environment.Exit(1);
        }

        string command = string.Join(" ", args);

        // Parse Command and process it
        string result = parser.HandleCommand(command);

        // Display result to user
        Console.WriteLine(result);
        
        client.Disconnect();

        Environment.Exit(0);
    }
    #endif
}
