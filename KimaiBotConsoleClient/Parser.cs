namespace KimaiAutoEntryCmdClient;

using CommandLine;
using KimaiBotService;

class Parser(PipeClient pipeClient)
{
    private PipeClient pipeClient = pipeClient;

    public int HandleCommand(string? command)
    {
        if(command == null)
            return -1;

        var args = command.Split();

        var result = CommandLine.Parser.Default.ParseArguments<LoginOptions, LogoutOptions, AddEntryOptions>(args)
            .MapResult(
                (LoginOptions opts) => RunLogin(opts),
                (LogoutOptions opts) => RunLogout(opts),
                (AddEntryOptions opts) => RunAddEntry(opts),
                errs => HandleParseError(errs)
            );

        return result;
    }

    private int RunLogin(LoginOptions opts)
    {
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
