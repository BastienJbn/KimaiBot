using KimaiAutoEntryCmdClient;
using KimaiBotService;

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
    var result = parser.HandleCommand(command);

    // Display result to user
    Console.WriteLine(result);
}

client.Disconnect();
