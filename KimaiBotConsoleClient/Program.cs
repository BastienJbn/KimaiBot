using KimaiBotCmdLine;
using System;

class Program
{
    // Main function when debugging
    #if DEBUG
    static void Main()
    {
        PipeClient client = new();
        Parser parser = new(client);

        Console.WriteLine("Start ! Type 'exit' to quit.");

        while (true)
        {
            // Wait for user input
            Console.Write(">");
            var command = Console.ReadLine();

            if (command is null)
                continue;

            if (command == "exit")
                break;

            // Parse Command and process it
            string result = parser.HandleCommand(command);

            // Display result to user
            Console.WriteLine(result);
        }
    }
#else

    // Main function when running the application
    static void Main(string[] args)
    {
        PipeClient client = new();
        Parser parser = new(client);

        if(args.Length == 0)
        {
            Environment.Exit(0);
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
