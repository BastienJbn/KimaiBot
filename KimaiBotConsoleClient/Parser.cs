namespace KimaiBotCmdLine;

using CommandLine;
using System;
using System.Collections.Generic;

class Parser(PipeClient pipeClient)
{
    private PipeClient pipeClient = pipeClient;

    public string HandleCommand(string command)
    {
        var args = command.Split();

        string result = CommandLine.Parser.Default.ParseArguments<LoginOptions, LogoutOptions, AddEntryOptions>(args)
            .MapResult(
                (LoginOptions opts) => ExecuteWithConnection(() => RunLogin(opts)),
                (LogoutOptions opts) => ExecuteWithConnection(() => RunLogout(opts)),
                (AddEntryOptions opts) => ExecuteWithConnection(() => RunAddEntry(opts)),
                errs => HandleParseError(errs)
            );

        pipeClient.Disconnect();

        return result;
    }

    private string ExecuteWithConnection(Func<string> runFunction)
    {
        if (!pipeClient.Connect())
        {
            return "Failed to connect to the pipe server!";
        }

        return runFunction();
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
        return pipeClient.SendReceive("addEntry");
    }

    private string HandleParseError(IEnumerable<Error> errs)
    {
        return "";
    }
}
