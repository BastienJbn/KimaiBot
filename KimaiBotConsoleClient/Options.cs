namespace KimaiAutoEntryCmdClient;

using CommandLine;

[Verb("login", HelpText = "Login to Kimai.")]
public class LoginOptions
{
    [Option('u', "username", Required = true, HelpText = "Username.")]
    public required string Username { get; set; }

    [Option('p', "password", Required = true, HelpText = "Password.")]
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
