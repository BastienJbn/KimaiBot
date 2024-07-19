namespace KimaiBotCmdLine;

using CommandLine;

[Verb("login", HelpText = "Login to Kimai.")]
public class LoginOptions
{
    [Value(1, HelpText = "Username.", Required = true)]
    public required string Username { get; set; }

    [Value(1, HelpText = "Password.", Required = true, Hidden = true)]
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
