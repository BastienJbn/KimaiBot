using System;
using System.IO;
using System.Text.Json;

namespace KimaiBotService;

public class UserPrefs
{
    private static readonly string FileName = "userprefs.json";
    // File path : location of the executable + FileName
    public static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FileName);

    private string? _username;
    private string? _password;
    private DateTime? _lastEntryAdded;
    private TimeSpan? _triggerTime;
    private int? _userId;

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

    public TimeSpan? TriggerTime
    {
        get => _triggerTime;
        set
        {
            _triggerTime = value;
            Save();
        }
    }

    public int? UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            Save();
        }
    }

    // Default constructor
    public UserPrefs()
    {
    }

    // Constructor with username and password parameters
    public UserPrefs(string username, string password)
    {
        _username = username;
        _password = password;
        Save();
    }

    private void Save()
    {
        // If file doesn't exist, create it
        if (!File.Exists(FilePath))
        {
            File.Create(FilePath).Close();
        }

        JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(this, serializerOptions);

        // Write the JSON string to the file
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
