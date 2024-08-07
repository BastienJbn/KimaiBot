using System;
using System.IO;
using System.Text.Json;

namespace KimaiBotService;

public class UserPrefs
{
    private string? _username;
    private string? _password;
    private DateTime? _lastEntryAdded;

    private static readonly string FilePath = "userprefs.json";

    public string? Username
    {
        get => _username;
        set
        {
            _username = value;
            Save();
        }
    }
    public string? Password
    {
        get => _password;
        set
        {
            _password = value;
            Save();
        }
    }
    public DateTime? LastEntryAdded
    {
        get => _lastEntryAdded;
        set
        {
            _lastEntryAdded = value;
            Save();
        }
    }

    // Constructeur par défaut
    public UserPrefs()
    {
    }

    // Constructeur avec paramètres username et password
    public UserPrefs(string username, string password)
    {
        _username = username;
        _password = password;
        Save();
    }

    private void Save()
    {
        JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(this, serializerOptions);

        // Écrire dans le fichier en écrasant le contenu précédent
        File.WriteAllText(FilePath, jsonString);
    }

    public static UserPrefs Load()
    {
        if (!File.Exists(FilePath))
            return new();

        string jsonString = File.ReadAllText(FilePath);
        UserPrefs? userPrefs = JsonSerializer.Deserialize<UserPrefs>(jsonString);

        return userPrefs ?? new();
    }
}
