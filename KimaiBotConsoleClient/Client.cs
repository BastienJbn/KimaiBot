namespace KimaiAutoEntryCmdClient;

using CommandLine;
using System.IO.Pipes;
using System.Text;

public class Client
{
    private readonly NamedPipeClientStream pipeClient = new(".", "KimaiAutoEntryPipe", PipeDirection.InOut);

    public Client()
    {
    }

    public bool Connect()
    {
        try
        {
            pipeClient.ConnectAsync(2000).Wait();
        }
        catch
        {
            return false;
        }

        return true;
    }

    public void Disconnect()
    {
        pipeClient.Close();
    }

    public int HandleCommand(string? command)
    {
        if(command == null)
            return -1;

        var args = command.Split();

        var result = Parser.Default.ParseArguments<LoginOptions, LogoutOptions, AddEntryOptions>(args)
            .MapResult(
                (LoginOptions opts) => RunLogin(opts),
                (LogoutOptions opts) => RunLogout(opts),
                (AddEntryOptions opts) => RunAddEntry(opts),
                errs => HandleParseError(errs)
            );

        return result;
    }

    public string SendCommand(string command)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(command);
        pipeClient.Write(buffer, 0, buffer.Length);

        buffer = new byte[256];
        int bytesRead = pipeClient.Read(buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    private int RunLogin(LoginOptions opts)
    {
        Console.WriteLine("Success");
        var exitCode = 0;
        var props = opts.GetType().GetProperties();
        //foreach (var prop in props)
        Console.WriteLine("Username: {0}", opts.Username);
        Console.WriteLine("Password: {0}", opts.Password);
        return exitCode;
    }

    private int RunLogout(LogoutOptions opts)
    {
        Console.WriteLine("Success");
        var exitCode = 0;
        return exitCode;
    }

    private int RunAddEntry(AddEntryOptions opts)
    {
        Console.WriteLine("Success");
        var exitCode = 0;
        return exitCode;
    }

    private int HandleParseError(IEnumerable<Error> errs)
    {
        var result = -2;
        Console.WriteLine("errors {0}", errs.Count());
        if (errs.Any(x => x is HelpRequestedError || x is VersionRequestedError))
            result = -1;
        Console.WriteLine("Exit code {0}", result);
        return result;
    }
}
