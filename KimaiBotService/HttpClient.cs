using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KimaiBotService;

class KimaiHttpClient
{
    private readonly HttpClient client;
    private readonly HttpClientHandler handler;

    private readonly string loginHttpAddress = "http://frapp01/kimai/index.php?a=checklogin";
    private readonly string processorHttpAddress = "http://frapp01/kimai/extensions/ki_timesheets/processor.php";
    private readonly string username = "";
    private readonly string password = "";

    public KimaiHttpClient()
    {
        handler = new HttpClientHandler
        {
            CookieContainer = new System.Net.CookieContainer()
        };

        client = new HttpClient(handler)
        {
            BaseAddress = new Uri(processorHttpAddress)
        };
    }

    public bool Authenticate()
    {
        var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("name", username),
            new KeyValuePair<string, string>("password", password)
        ]);

        var response = client.PostAsync(loginHttpAddress, content).Result;

        return response.IsSuccessStatusCode;
    }

    public void Logout()
    {
        handler.CookieContainer = new System.Net.CookieContainer();
    }

    public bool AddEntryComboRnD()
    {
        var payload = new List<KeyValuePair<string, string>>
        {
            new("axAction",    "add_edit_timeSheetEntry"),
            new("projectID",   "18"),
            new("activityID",  "52"),
            new("description", ""),
            new("start_day",   DateTime.Now.ToString("dd.MM.yyyy")),
            new("end_day",     DateTime.Now.ToString("dd.MM.yyyy")),
            new("start_time",  "00:00:00"),
            new("end_time",    "07:00:00"),
            new("duration",    "07:00:00"),
            new("comment",     ""),
            new("commentType", "0"),
            new("userID[]",    "675906454"),
            new("statusID",    "1"),
            new("billable",    "0")
        };

        var content = new FormUrlEncodedContent(payload);
        var response = client.PostAsync(processorHttpAddress, content).Result;

        return response.IsSuccessStatusCode;
    }
}
