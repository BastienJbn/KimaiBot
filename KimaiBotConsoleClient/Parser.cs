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

        pipeClient.Connect();

        // TODO: Make the process of new Options automatic, without having to create new functions each time.
        string result = CommandLine.Parser.Default.ParseArguments<
              LoginOptions
            , LogoutOptions
            , AddEntryOptions
#if DEBUG
            , IntervalOptions
#endif   
            >(args)
            .MapResult(
                (LoginOptions opts) => RunLogin(opts)
                , (LogoutOptions opts) => RunLogout(opts)
                , (AddEntryOptions opts) => RunAddEntry(opts)
#if DEBUG
                , (IntervalOptions opts) => RunInterval(opts)
#endif
                , errs => HandleParseError(errs)
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

#if DEBUG
    private string RunInterval(IntervalOptions opts)
    {
        return pipeClient.SendReceive($"interval {opts.val}");
    }
#endif

    private string HandleParseError(IEnumerable<Error> errs)
    {
        return "";
    }
}
