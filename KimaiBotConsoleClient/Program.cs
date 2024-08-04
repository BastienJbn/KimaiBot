using KimaiBotCmdLine;
using System;
using System.IO;
using System.Reflection;

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
    // Add the path of the executable to the system environment variables
    string directory = AppContext.BaseDirectory;
    //Remove the last backslash
    directory = directory.Remove(directory.Length - 1);

    // Check if the executable exists
    string executablePath = Path.Combine(directory, $"{Assembly.GetExecutingAssembly().GetName().Name}.exe");
    if (!File.Exists(executablePath))
    {
        Console.WriteLine("Executable not found");
        Environment.Exit(1);
    }

    // Add the path of the executable to the system environment variables
    Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";" + directory);

    Environment.Exit(0);
}
else if (args.Length != 0 && args[0] is "/Uninstall")
{
    // Remove the directory of the executable from the system environment variables
    string directory = AppContext.BaseDirectory;
    //Remove the last backslash
    directory = directory.Remove(directory.Length - 1);

    // Check if the executable exists
    string executablePath = Path.Combine(directory, $"{Assembly.GetExecutingAssembly().GetName().Name}.exe");
    if (!File.Exists(executablePath)) {
        Console.WriteLine("Executable not found");
        Environment.Exit(1);
    }

    // Remove the path of the executable from the system environment variables
    string? path = Environment.GetEnvironmentVariable("PATH");
    if (path != null)
    {
        path = path.Replace(directory, "");
        Environment.SetEnvironmentVariable("PATH", path);
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