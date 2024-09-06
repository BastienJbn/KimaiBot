using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Timers;
using System.ServiceProcess;
using Microsoft.Win32;

namespace KimaiBotService;

[SupportedOSPlatform("windows")]
public class KimaiBot : BackgroundService
{
    private readonly ILogger<KimaiBot> logger;

    private readonly System.Timers.Timer timer = new();
    private readonly KimaiHttpClient httpClient = new();
    private readonly PipeServer server = new();

    private UserPrefs userPrefs = new();

    public KimaiBot(ILogger<KimaiBot> _logger)
    {
        logger = _logger;

        // Subscribe to power mode change events
        SystemEvents.PowerModeChanged += OnPowerChange;

        SafeSetDefaultConfig();
    }

    // Unsubscribe from events when service stops
    public override void Dispose()
    {
        SystemEvents.PowerModeChanged -= OnPowerChange;
        timer.Dispose();
        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogWarning("LogLevel Trace is not enabled!");
        }

        logger.LogTrace("Démarrage service KimaiBot.");

        // Load user preferences and set to default values
        userPrefs = UserPrefs.Load();

        // Init timer
        timer.Elapsed += OnTimerElapsed;
        timer.AutoReset = false;

        // Add entry if needed and restart timer
        SafeAddEntry();

        // Start command handler and server
        await Task.WhenAll(CommandHandler(token), server.Start(token));
    }

    /**
     * Handle commands received from the client.
     * @param token The cancellation token.
     */
    private async Task CommandHandler(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            string request = await server.GetRequestAsync(token);

            if (string.IsNullOrEmpty(request))
            {
                continue;
            }

            string response = HandleCommand(request);
            server.SendResponse(response);
        }
    }

    private TimeSpan GetTimeUntilTrigger()
    {
        if (userPrefs.AddTime == null)
            return new(0);

        TimeSpan timeUntilTrigger = userPrefs.AddTime.Value - DateTime.Now.TimeOfDay;

        // If time is negative or entry was already added today, add a day
        if (timeUntilTrigger.TotalMilliseconds < 0 || userPrefs.LastEntryAdded?.Day == DateTime.Now.Day)
            timeUntilTrigger = timeUntilTrigger.Add(new TimeSpan(1, 0, 0, 0));

        return timeUntilTrigger;
    }

    /**
     * Start timer with user pref interval
     */
    private void StartTimer()
    {
        // Calculate time until trigger
        TimeSpan timeUntilTrigger = GetTimeUntilTrigger();

        // Start timer if not already started
        timer.Interval = timeUntilTrigger.TotalMilliseconds;

        if (!timer.Enabled)
            timer.Start();
    }

    private DateTime GetNextTriggerTime()
    {
        return DateTime.Now.Add(GetTimeUntilTrigger() + TimeSpan.FromSeconds(1));
    }

    /**
     * Handle the command received from the client.
     * @param request The command received from the client.
     * @return The response to the client.
     */
    private string HandleCommand(string request)
    {
        string[] args = request.Split(' ');

        if (args.Length == 0)
        {
            Console.WriteLine("Received empty command.");
            return "No command provided.";
        }

        switch (args[0])
        {
            case "login":
                // Save credential
                userPrefs.Username = args[1];
                userPrefs.Password = args[2];

                logger.LogWarning("Tentative d'authentification...");

                int userId = httpClient.Authentify(userPrefs.Username, userPrefs.Password);

                if (userId > 0)
                {
                    logger.LogWarning("Authentification réussie.");

                    // Add entry if needed and restart timer
                    SafeAddEntry();

                    return "Successfully logged in.";
                }
                else
                {
                    logger.LogError("Authentification échouée.");

                    return "Failed to log in.";
                }

            case "logout":
                // Clear user credentials
                userPrefs.Username = null;
                userPrefs.Password = null;

                // Log out of Kimai
                httpClient.Logout();

                // Stop timer
                timer.Enabled = false;

                logger.LogWarning("Utilisateur déconnecté.");
                return "Successfully logged out.";

            case "addEntry":
                if (userPrefs.Username == null || userPrefs.Password == null)
                {
                    logger.LogWarning("Utilisateur non authentifié.");
                    return "User not authenticated. Use \"login\" command before.";
                }

                if (AddEntry())
                    return "Successfully added entry.";
                else
                    return "Failed to add entry.";

            case "configure":
                TimeSpan _startTime, _duration, _addTime;

                if (!TimeSpan.TryParse(args[1], out _startTime))
                    return "Wrong time format for [StartTime].";

                if (!TimeSpan.TryParse(args[2], out _duration))
                    return "Wrong time format for [Duration].";

                if (!TimeSpan.TryParse(args[3], out _addTime))
                    return "Wrong time format for [AddTime].";

                userPrefs.StartTime = _startTime;
                userPrefs.Duration = _duration;
                userPrefs.AddTime = _addTime;

                // If addTime is prior in the day that current time
                if(_addTime < DateTime.Today - DateTime.Now)
                {
                    // Add entry
                    SafeAddEntry();
                }

                StartTimer();

                return "Configuration saved.";

            case "status":
                string ret = "";

                if (CredsEntered())
                    ret += $"Logged as {userPrefs.Username}\n\r";
                else
                    ret += "Not logged yet.\n\r";
                ret += $"Next trigger: {GetNextTriggerTime()}\n\r";
                ret += "Entry Configuration: \n\r";
                ret += $"\tStart time: {userPrefs.StartTime}\n\r";
                ret += $"\tDuration: {userPrefs.Duration}";

                return ret;

#if DEBUG
            case "interval":
                int val = int.Parse(args[1]);
                timer.Interval = val;
                timer.Enabled = true;
                return $"interval set to {val}";
#endif

            default:
                logger.LogWarning("Commande reçue invalide.");
                return "Invalid command.";
        }
    }

    private void OnTimerElapsed(object? sender, System.Timers.ElapsedEventArgs? e)
    {
        logger.LogWarning("Timer triggered periodic task at: {time}", DateTime.Now);

        // Add entry if needed and restart timer
        SafeAddEntry();
    }

    private bool SafeAddEntry()
    {
        // Last entry was today ?
        if ((userPrefs.LastEntryAdded != null) && userPrefs.LastEntryAdded.Value.Date == DateTime.Now.Date)
        {
            logger.LogWarning("Entrée ajoutée. Démarrage du timer pour un déclanchement à {heure}.", userPrefs.AddTime.ToString());
            StartTimer();
            return true;
        }
        
        if(AddEntry())
        {
            logger.LogWarning("Entrée ajoutée. Démarrage du timer pour un déclanchement à {heure}.", userPrefs.AddTime.ToString());
            StartTimer();
            return true;
        }
        else
        {
            logger.LogError("Ajout d'entrée échouée.");
            timer.Stop();

            // TODO: windows notif. Probably a server disconnection.

            return false;
        }
    }

    private bool AddEntry()
    {
        // Check creds
        if (!CredsEntered())
            return false;

        // Try to authenticate
        int userId = httpClient.Authentify(userPrefs.Username, userPrefs.Password);

        if (userId <= 0)
        {
            logger.LogError("Authentification échouée.");

            return false;
        }

        if (httpClient.AddEntryComboRnD(userId, userPrefs.StartTime.Value, userPrefs.Duration.Value))
        {
            userPrefs.LastEntryAdded = DateTime.Now;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            logger.LogInformation("System is going to sleep.");
            // Handle system suspend logic, e.g., pause timers or save state
        }
        else if (e.Mode == PowerModes.Resume)
        {
            logger.LogInformation("System has resumed from sleep.");

            // Add entry if needed and restart timer
            SafeAddEntry();
        }
    }

    private bool CredsEntered()
    {
        return userPrefs.Username != null && userPrefs.Password != null;
    }

    private void SafeSetDefaultConfig()
    {
        string logStr = "";

        if (userPrefs.StartTime == null)
        {
            userPrefs.StartTime = new(0, 0, 0);
            logStr += $"StartTime non définie. {userPrefs.StartTime} par défaut.\n\r";
        }

        if (userPrefs.Duration == null)
        {
            userPrefs.Duration = new(7, 24, 0);
            logStr += $"Durée non définie. {userPrefs.Duration} par défaut.\n\r";
        }

        if (userPrefs.AddTime == null)
        {
            userPrefs.AddTime = new TimeSpan(0, 1, 0);
            logStr += $"Heure de déclenchement non définie. {userPrefs.AddTime} par défaut.\n\r";
        }

        logger.LogWarning(logStr);
    }
}