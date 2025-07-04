using System.Globalization;
using EasyRCP.Forms;
using EasyRCP.Services;
using Microsoft.Win32;

namespace EasyRCP;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static async Task Main()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("pl-PL");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("pl-PL");

        try
        {
            // Check for new application version and apply it if available
            await GitHubUpdater.CheckVersionAndUpdateApplicationAsync();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            AddApplicationToStartup();
            ApplicationConfiguration.Initialize();

            var credentials = UserCredentialsService.LoadCredentials();
            if (credentials == null || string.IsNullOrEmpty(credentials.Value.Email) || string.IsNullOrEmpty(credentials.Value.Password))
            {
                MessageBox.Show(
                    "Brak loginu i has³a, proszê nacisn¹æ OK i uzupe³niæ dane.",
                    "EasyRCP - Brak loginu i has³a",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);


                var form = new MainForm(isHidden: false);
                var dialogResult = form.ShowDialog(); // run as ShowDialog instead of using Application.Run so that the application comes back here once form is closed
                if (dialogResult == DialogResult.Cancel)
                {
                    // Cancelled (X button clicked) - exit the app
                    Application.Exit();
                    return;
                }
            }

            RcpApiClient? api;
            do
            {
                // Downloading credentials again
                credentials = UserCredentialsService.LoadCredentials();
                if (credentials == null || string.IsNullOrEmpty(credentials.Value.Email) || string.IsNullOrEmpty(credentials.Value.Password))
                {
                    throw new InvalidOperationException("Coœ posz³o nie tak, nie uda³o siê za³adowaæ emailu i has³a mimo, ¿e powinny byæ ju¿ ustawione");
                }

                // Initialize the api client with the loaded credentials to comunicate with RCP system
                // This method also tries to log in the user
                api = await RcpApiClient.CreateApiClientAsync(credentials.Value.Email, credentials.Value.Password);
                if (!api.LoginSuccessful)
                {
                    MessageBox.Show(
                        "Nie uda³o siê zalogowaæ do aplikacji - b³êdny email lub has³o. Proszê nacisn¹æ OK i spróbowaæ ponownie.",
                        "EasyRCP - B³¹d",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    var form = new MainForm(isHidden: false);
                    var dialogResult = form.ShowDialog(); // run as ShowDialog instead of using Application.Run so that the application comes back here once form is closed
                    if (dialogResult == DialogResult.Cancel)
                    {
                        // Cancelled (X button clicked) - exit the app
                        Application.Exit();
                        return;
                    }
                }
            } while (!api.LoginSuccessful);

            

            // Check if the work has already started - runs in the background 6 times every 1 minute
            var result = await RcpAutomationService.CheckIfWorkAlreadyStartedWithRetryAsync(api);
            if (result == null)
            {
                MessageBox.Show(
                    "Brak po³¹czenia z internetem. Aplikacja dzia³a w tle i bêdzie dostêpna po wznowieniu po³¹czenia z internetem.",
                    "EasyRCP - Brak Internetu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Application.Run(new MainForm(isHidden: true, rcpApi: api)); // running the hidden main form
                return;
            }
            else if (result == false)
            {
                using var prompt = new StartWorkPromptForm();
                if (prompt.ShowDialog() == DialogResult.Yes)
                {
                    await RcpAutomationService.StartWorkAsync(api);
                }
            }

            Application.Run(new MainForm(isHidden: true, rcpApi: api)); // running the hidden main form
        }
        catch (Exception ex)
        {
            // TODO: tutaj mo¿e mail jeszcze do mnie z informacj¹ ¿e coœ posz³o komuœ nie tak - komu i co posz³o nie tak
            File.AppendAllText("output.txt", $"[{DateTime.Now}] {ex}\n\n");
            MessageBox.Show(
                "Wyst¹pi³ nieoczekiwany b³¹d. Szczegó³y zapisano w pliku output.txt",
                "EasyRCP - B³¹d",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    static void AddApplicationToStartup()
    {
        string appName = "EasyRcp";
        string exePath = Application.ExecutablePath;

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (key != null && key.GetValue(appName) == null)
        {
            key.SetValue(appName, exePath);
        }
    }
}
