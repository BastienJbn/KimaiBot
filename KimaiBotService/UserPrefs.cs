namespace KimaiBotService;

public class UserPrefs
{
    public string Username { get; set; }
    public string Password { get; set; }

    public UserPrefs(string username, string password)
    {
        Username = username;
        Password = password;
    }
}
