namespace KimaiBotCmdLine;

using CommandLine;
using System;
using System.Collections.Generic;

class Parser(PipeClient pipeClient)
{
    public string HandleCommand(string command)
    {
        var args = command.Split();

        pipeClient.Connect();

        // TODO: Make the process of new Options automatic, without having to create new functions each time.
        string result = CommandLine.Parser.Default.ParseArguments<
              LoginOptions
            , LogoutOptions
            , AddEntryOptions
            , ConfigureOptions
#if DEBUG
            , IntervalOptions
#endif   
            >(args)
            .MapResult(
                (LoginOptions opts) => RunLogin(opts)
                , (LogoutOptions opts) => RunLogout(opts)
                , (AddEntryOptions opts) => RunAddEntry(opts)
                , (ConfigureOptions opts) => RunConfigure(opts) 
#if DEBUG
                , (IntervalOptions opts) => RunInterval(opts)
#endif
                , errs => HandleParseError(errs)
            );

        pipeClient.Disconnect();

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
        return pipeClient.SendReceive("addEntry");
    }

    private string RunConfigure(ConfigureOptions opts)
    {
        return pipeClient.SendReceive($"configure {opts.StartTime} {opts.Duration} {opts.AddTime}");
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
