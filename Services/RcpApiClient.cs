using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace EasyRCP.Services;

/// <summary>
/// A client for interacting with the RCP Online via api requests.
/// </summary>
public class RcpApiClient
{
    private readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="RcpApiClient"/> class.
    /// </summary>
    /// <param name="phpSessionId">The PHP session ID used for authentication.</param>
    public RcpApiClient(string? phpSessionId = null)
    {
        // Cookie container handler to add PHPSESSID cookie
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = true,
        };

        if (phpSessionId != null)
        {
            handler.CookieContainer.Add(new Uri("https://panel.rcponline.pl"),
                new Cookie("PHPSESSID", phpSessionId));
        }

        _client = new HttpClient(handler);
        _client.BaseAddress = new Uri("https://panel.rcponline.pl");

        // Headers from the browser to exactly mimic everything
        _client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
        _client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pl-PL,pl;q=0.9");
        _client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        _client.DefaultRequestHeaders.Referrer = new Uri("https://panel.rcponline.pl/app/zdarzenia");
        _client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
    }

    private string? ExtractCsrfToken(string html)
    {
        var pattern = "name=\"_csrf_token\"[^>]*value=\"(.*?)\""; // pattern to find the csrf token needed for login
        var match = Regex.Match(html, pattern);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Sends a start work clock event to the RCP API.
    /// </summary>
    /// <param name="email">The employee email.</param>
    /// <param name="password">The employee password.</param>
    /// <returns>If login successful the application html is returned; otherwise null.</returns>
    public async Task<string?> SendLoginEventAsync(string email, string password)
    {
        var loginPageResp = await _client.GetAsync("/login/");
        string json = await ReadResponseAsDecompressedString(loginPageResp);

        var csrfToken = ExtractCsrfToken(json);
        if (csrfToken == null)
        {
            Console.WriteLine("CSRF token not found!");
            return null;
        }

        var data = new[]
        {
            new KeyValuePair<string,string>("_username", email),
            new KeyValuePair<string,string>("_password", password),
            new KeyValuePair<string,string>("_csrf_token", csrfToken)
        };

        var content = new FormUrlEncodedContent(data);

        var loginResp = await _client.PostAsync("/login_check/", content);
        string loginHtml = await ReadResponseAsDecompressedString(loginResp);

        var appResp = await _client.GetAsync("/app/");
        string appHtml = await ReadResponseAsDecompressedString(appResp);

        if (loginHtml.Contains("Panel | RCPonline") && appHtml.Contains("Panel | RCPonline"))
        {
            // If login successful then the returned html in both login and app endpoints
            // is just the application itself and contains "Panel | RCPonline"
            return appHtml;
        }
        else if (loginHtml.Contains("Zaloguj się w systemie rejestracji czasu pracy - RCPonline | RCPonline")
                 && appHtml.Contains("Zaloguj się w systemie rejestracji czasu pracy - RCPonline | RCPonline"))
        {
            // If login unsuccessful then the returned html in both login and app endpoints
            // is just the login view
            return null;
        }
        else
        {
            // Sth not right, return false
            return null;
        }
    }

    internal async Task<bool> CheckIfWorkAlreadyStarted(string email, string password)
    {
        var applicationHtml = await SendLoginEventAsync(email, password);
        if (applicationHtml == null)
        {
            // Login unsuccessful TODO: trzeba obsłużyć jakoś, zastanowić się, czy logowanie nie powinno być np w konstruktorze albo chociaż w innej metodzie
            return false;
        }

        var myStatusResponse = await _client.PostAsync("/dashboard/getMyStatus/1", null);
        string json = await ReadResponseAsDecompressedString(myStatusResponse);
        var parsed = JsonDocument.Parse(json);
        var rawHtml = parsed.RootElement.GetProperty("body").GetString();
        var readableHtml = WebUtility.HtmlDecode(rawHtml);

        if (readableHtml != null && (readableHtml.Contains("Na stanowisku")
                                    || readableHtml.Contains("Praca zdalna")
                                    || readableHtml.Contains("W terenie")))
        {
            // User is already at work
            return true;
        }
        else if (readableHtml != null && readableHtml.Contains("Nie ma"))
        {
            // User is not at work
            return false;
        }
        else
        {
            // Unexpected response TODO: trzeba obsłużyć jakoś
            return false;
        }
    }

    /// <summary>
    /// Sends a start work clock event to the RCP API.
    /// </summary>
    /// <param name="empId">The employee ID.</param>
    /// <param name="zone">The zone ID.</param>
    /// <param name="eventTypeId">The event type ID (start work/end work/...).</param>
    /// <param name="project">The project name.</param>
    /// <param name="remote">Indicates whether the event is remote (1 for true, 0 for false).</param>
    /// <returns>True if success; otherwise, false.</returns>
    public async Task<bool> SendClockEventAsync(
        int empId,
        int zone,
        int eventTypeId,
        string project,
        int remote)
    {
        var data = new[]
        {
            new KeyValuePair<string,string>("empId", empId.ToString()),
            new KeyValuePair<string,string>("zone", zone.ToString()),
            new KeyValuePair<string,string>("eventTypeId", eventTypeId.ToString()),
            new KeyValuePair<string,string>("comment", ""),
            new KeyValuePair<string,string>("project", project),
            new KeyValuePair<string,string>("gps", ""), // 51.1082496;16.9803776
            new KeyValuePair<string,string>("remote", remote.ToString()),
            new KeyValuePair<string,string>("photo", ""),
            new KeyValuePair<string,string>("customTime", "")
        };

        var content = new FormUrlEncodedContent(data);
        var success = await SendPostAndHandleResponseAsync("/event/clockEvent", content);
        return success;
    }

    /// <summary>
    /// Sends a project event to the API.
    /// </summary>
    /// <param name="empId">The employee ID.</param>
    /// <param name="zone">The zone ID.</param>
    /// <param name="eventTypeId">The event type ID (start work/end work/...).</param>
    /// <param name="project">The project name.</param>
    /// <param name="remote">Indicates whether the event is remote (1 for true, 0 for false).</param>
    /// <returns>True if success; otherwise, false.</returns>
    public async Task<bool> SendProjectEventAsync(
        int empId,
        int zone,
        int eventTypeId,
        string project,
        int remote)
    {
        var data = new[]
        {
            new KeyValuePair<string,string>("empId", empId.ToString()),
            new KeyValuePair<string,string>("zone", zone.ToString()),
            new KeyValuePair<string,string>("eventTypeId", eventTypeId.ToString()),
            new KeyValuePair<string,string>("comment", ""),
            new KeyValuePair<string,string>("project", project),
            new KeyValuePair<string,string>("remote", remote.ToString()),
            new KeyValuePair<string,string>("gps", ""), // 51.1082496;16.9803776
            new KeyValuePair<string,string>("photo", ""),
            new KeyValuePair<string,string>("customTime", "")
        };

        var content = new FormUrlEncodedContent(data);
        var success = await SendPostAndHandleResponseAsync("/event/projectEvent", content);
        return success;
    }

    /// <summary>
    /// Sends a POST request to the specified URI and handles the response.
    /// </summary>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="content">The content to send in the POST request.</param>
    /// <returns>True if success; otherwise, false.</returns>
    private async Task<bool> SendPostAndHandleResponseAsync(string requestUri, FormUrlEncodedContent content)
    {
        var resp = await _client.PostAsync(requestUri, content);
        string json = await ReadResponseAsDecompressedString(resp);

        if (!resp.IsSuccessStatusCode)
        {
            File.AppendAllText("output.txt", $"[{DateTime.Now}] HTTP {(int)resp.StatusCode}: {resp.ReasonPhrase}\n{json}\n\n");
            MessageBox.Show(
                $"Błąd, szczegóły zostały zapisane w pliku output.txt",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return false;
        }

        using var doc = JsonDocument.Parse(json);
        bool success = doc.RootElement.GetProperty("success").GetBoolean();
        if (!success)
        {
            File.AppendAllText("output.txt", $"[{DateTime.Now}] API zwróciło success = false\n{json}\n\n");
            MessageBox.Show(
                $"Błąd, szczegóły zostały zapisane w pliku output.txt",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        return success;
    }

    // method to read response as readable json
    private async Task<string> ReadResponseAsDecompressedString(HttpResponseMessage response)
    {
        using var responseStream = await response.Content.ReadAsStreamAsync();
        using var decompressed = new System.IO.Compression.GZipStream(responseStream, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(decompressed);
        string stringContent = await reader.ReadToEndAsync();
        return stringContent;
    }
}