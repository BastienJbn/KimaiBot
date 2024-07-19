namespace KimaiBotCmdLine;

using CommandLine;

class Parser(PipeClient pipeClient)
{
    private PipeClient pipeClient = pipeClient;

    public string HandleCommand(string command)
    {
        var args = command.Split();

        string result = CommandLine.Parser.Default.ParseArguments<LoginOptions, LogoutOptions, AddEntryOptions>(args)
            .MapResult(
                (LoginOptions opts) => RunLogin(opts),
                (LogoutOptions opts) => RunLogout(opts),
                (AddEntryOptions opts) => RunAddEntry(opts),
                errs => HandleParseError(errs)
            );

        return result;
    }

    private string RunLogin(LoginOptions opts)
    {
        return pipeClient.SendReceive($"login {opts.Username} {opts.Password}");
    }

    private string RunLogout(LogoutOptions opts)
    {
        return pipeClient.SendReceive("logout");
    }

    private string RunAddEntry(AddEntryOptions opts)
    {
        return "Success";
    }

    private string HandleParseError(IEnumerable<Error> errs)
    {
        // Nothing to display. CommandLine library will display the error.
        return "";
    }
}
