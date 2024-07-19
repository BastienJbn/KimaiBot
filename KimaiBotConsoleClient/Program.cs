using KimaiAutoEntryCmdClient;

Client client = new();

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

    // Send command to server
    var result = client.HandleCommand(command);

    Console.WriteLine(result);
}

client.Disconnect();
