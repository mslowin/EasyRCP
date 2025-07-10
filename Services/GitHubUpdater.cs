using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace EasyRCP.Services;

/// <summary>
/// Provides functionality to check for and apply updates from the latest GitHub release.
/// </summary>
public static class GitHubUpdater
{
    private const string GitHubApiUrl = "https://api.github.com/repos/mslowin/EasyRCP/releases/latest";

    private const string ExecutableName = "EasyRCP.exe";

    /// <summary>
    /// Checks for a new release on GitHub and updates the application if a newer version is available.
    /// Downloads the new executable and initiates a script to replace the running instance.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task CheckVersionAndUpdateApplicationAsync()
    {
        var hasUpdateHappened = CheckIfUpdateScriptExists();
        if (hasUpdateHappened)
        {
            return;
        }

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("EasyRCP");

        var json = await httpClient.GetStringAsync(GitHubApiUrl);
        var release = JsonSerializer.Deserialize<GitHubRelease>(json);

        var currentAppVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?.Split('+')[0];

        var latestAppVersion = release?.TagName?.TrimStart('v');

        if (latestAppVersion != null && currentAppVersion != null && latestAppVersion != currentAppVersion)
        {
            var asset = release?.Assets?.Find(a => a.ApplicationName == ExecutableName);
            if (asset != null)
            {
                MessageBox.Show(
                    $"Znaleziono nową wersję aplikacji: {latestAppVersion}.\n" +
                    $"Aktualna wersja to: {currentAppVersion}.\n" +
                    $"Aplikacja zostanie zaktualizowana do najnowszej wersji.",
                    "EasyRCP - nowa wersja",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                var appDir = AppContext.BaseDirectory;
                var newExePath = Path.Combine(appDir, "EasyRCP_new.exe");
                using var stream = await httpClient.GetStreamAsync(asset.AplicationDownloadUrl);
                using var fs = File.Create(newExePath);
                await stream.CopyToAsync(fs);

                CreateAndRunUpdateScript();
                Application.Exit();
            }
        }
    }

    /// <summary>
    /// Checks if an update script exists and, if so,
    /// displays a message indicating that the application has been updated. Deletes the script after.
    /// </summary>
    /// <returns>True if the update script exists and was processed, false otherwise.</returns>
    private static bool CheckIfUpdateScriptExists()
    {
        var updateScriptPath = Path.Combine(AppContext.BaseDirectory, "update.bat");
        File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "output.txt"), updateScriptPath);

        if (File.Exists(updateScriptPath))
        {
            MessageBox.Show(
                "Aplikacja została zaktualizowana do najnowszej wersji i automatycznie się uruchomi.",
                "EasyRCP - Aktualizacja zakończona",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            File.Delete(updateScriptPath);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a batch script to terminate the current process, replace the executable, and restart the application.
    /// Executes the script in a new process.
    /// </summary>
    private static void CreateAndRunUpdateScript()
    {
        var appDirectory = AppContext.BaseDirectory;
        var exePath = Path.Combine(appDirectory, "EasyRCP.exe");
        var newExePath = Path.Combine(appDirectory, "EasyRCP_new.exe");
        var updateScriptPath = Path.Combine(appDirectory, "update.bat");

        var script = $"""
            taskkill /f /im EasyRCP.exe
            timeout /t 2 /nobreak > NUL
            del "{exePath}"
            timeout /t 1 /nobreak > NUL
            rename "{newExePath}" "EasyRCP.exe"
            timeout /t 2 /nobreak > NUL
            start "" "{exePath}"
            """;

        File.WriteAllText(updateScriptPath, script);
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{updateScriptPath}\"") { CreateNoWindow = false });
    }

    /// <summary>
    /// Represents a GitHub release with its tag and associated assets.
    /// </summary>
    private sealed class GitHubRelease
    {
        /// <summary>
        /// Gets or sets the tag name of the release (e.g., "v1.2.3").
        /// </summary>
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        /// <summary>
        /// Gets or sets the list of assets attached to the release.
        /// </summary>
        [JsonPropertyName("assets")]
        public List<GitHubAsset>? Assets { get; set; }
    }

    /// <summary>
    /// Represents an asset in a GitHub release.
    /// </summary>
    private sealed class GitHubAsset
    {
        /// <summary>
        /// Gets or sets the name of the asset (e.g., "EasyRCP.exe").
        /// </summary>
        [JsonPropertyName("name")]
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the download URL for the asset.
        /// </summary>
        [JsonPropertyName("browser_download_url")]
        public string? AplicationDownloadUrl { get; set; }
    }
}
