using KimaiBotCmdLine;
using System;

const string pathKey = "PATH";

// Main function when debugging
#if DEBUG

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

#else

// Main function when running the application

if (args.Length!=0 && args[0] is "/Install")
{
    string directory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
    string? currentPath = Environment.GetEnvironmentVariable(pathKey, EnvironmentVariableTarget.Machine);

    if (currentPath != null && !currentPath.Contains(directory))
    {
        Environment.SetEnvironmentVariable(pathKey, currentPath + ";" + directory, EnvironmentVariableTarget.Machine);
    }

    Environment.Exit(0);
}
else if (args.Length != 0 && args[0] is "/Uninstall")
{
    string directory = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
    string? currentPath = Environment.GetEnvironmentVariable(pathKey, EnvironmentVariableTarget.Machine);

    if (currentPath != null && currentPath.Contains(directory))
    {
        string newPath = currentPath.Replace(directory, "").Replace(";;", ";");
        Environment.SetEnvironmentVariable(pathKey, newPath, EnvironmentVariableTarget.Machine);
    }

    Environment.Exit(0);
}

PipeClient client = new();
Parser parser = new(client);

string command = string.Join(" ", args);

// Parse Command and process it
string result = parser.HandleCommand(command);

// Display result to user
Console.WriteLine(result);

client.Disconnect();

Environment.Exit(0);

#endif