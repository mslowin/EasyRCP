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
        try
        {
#if DEBUG
            string environment = "Development";
#else
            string environment = "Production";
#endif
            SentrySdk.Init(o =>
            {
                o.Dsn = "https://fc312c602b39fe2f9c974049fa40bc6f@o4509757627367424.ingest.de.sentry.io/4509768557068368";
#if DEBUG
                o.Debug = true;
#else
                o.Debug = false;
#endif
                o.Environment = environment;
                o.TracesSampleRate = 1.0;
            });

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

            // Ensure the application is running from the correct location on the PC
            SelfRelocationService.EnsureRunningFromCorrectLocation();

            // Check for new application version and apply it if available - runs in the background 6 times every 1 minute
            var success = await RcpAutomationService.CheckGitVersionAndUpdateApplicationWithRetryAsync();
            if (!success)
            {
                MessageBox.Show(
                    "Brak połączenia z internetem.",
                    "EasyRCP - Brak Internetu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            AddApplicationToStartup();
            ApplicationConfiguration.Initialize();

            var credentials = UserCredentialsService.LoadCredentials();
            if (credentials == null || string.IsNullOrEmpty(credentials.Value.Email) || string.IsNullOrEmpty(credentials.Value.Password))
            {
                MessageBox.Show(
                    "Brak loginu i hasła, proszę nacisnąć OK i uzupełnić dane.",
                    "EasyRCP - Brak loginu i hasła",
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
                    throw new InvalidOperationException("Coś poszło nie tak, nie udało się załadować emailu i hasła mimo, że powinny być już ustawione");
                }

                // Initialize the api client with the loaded credentials to comunicate with RCP system
                // This method also tries to log in the user - runs in the background 6 times every 1 minute
                api = await RcpAutomationService.CreateApiClientWithRetryAsync(credentials.Value.Email, credentials.Value.Password);
                if (api == null)
                {
                    MessageBox.Show(
                        "Brak połączenia z internetem.",
                        "EasyRCP - Brak Internetu",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                if (!api.LoginSuccessful)
                {
                    MessageBox.Show(
                        "Nie udało się zalogować do aplikacji - błędny email lub hasło. Proszę nacisnąć OK i spróbować ponownie.",
                        "EasyRCP - Błąd",
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
                    "Brak połączenia z internetem. Aplikacja działa w tle i będzie dostępna po wznowieniu połączenia z internetem.",
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
            SentrySdk.CaptureException(ex);
            File.AppendAllText("output.txt", $"[{DateTime.Now}] {ex}\n\n");
            MessageBox.Show(
                "Wystąpił nieoczekiwany błąd. Szczegóły zapisano w pliku output.txt, proszę skonsultować się z administratorem.",
                "EasyRCP - Błąd",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            SentrySdk.Close();
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
