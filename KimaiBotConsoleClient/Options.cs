namespace KimaiBotCmdLine;

using CommandLine;

[Verb("login", HelpText = "Login to Kimai.")]
public class LoginOptions
{
    [Value(1, HelpText = "Username.", Required = true)]
    public required string Username { get; set; }

    [Value(2, HelpText = "Password.", Required = true)]
    public required string Password { get; set; }
}

[Verb("logout", HelpText = "Logout from Kimai.")]
public class LogoutOptions
{
}

[Verb("addEntry", HelpText = "Add entry to Kimai.")]
public class AddEntryOptions
{
}

[Verb("configure", HelpText = "Configure entry parameters.")]
public class ConfigureOptions
{
    [Value(1, HelpText = "Start time of the entry. Format is 00:00 (hour:min).", Required = true)]
    public required string StartTime { get; set; }

    [Value(2, HelpText = "Duration of the entry. Format is 00:00 (hour:min)", Required = true)]
    public required string Duration { get; set; }

    [Value(3, HelpText = "The time at which the entry should be added by the bot. Format is 00:00 (hour:min)", Required = true)]
    public required string AddTime { get; set; }
}

// Debug commands
#if DEBUG
[Verb("interval", HelpText = "set timer interval")]
public class IntervalOptions
{
    [Value(1, HelpText = "time in ms.", Required = true)]
    public required int val { get; set; }
}
#endif
