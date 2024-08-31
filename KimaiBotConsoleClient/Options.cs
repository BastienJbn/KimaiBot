﻿namespace KimaiBotCmdLine;

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

// Debug commands
#if DEBUG
[Verb("interval", HelpText = "set timer interval")]
public class IntervalOptions
{
    [Value(1, HelpText = "time in ms.", Required = true)]
    public required int val { get; set; }
}
#endif
