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
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            AddApplicationToStartup();
            ApplicationConfiguration.Initialize();

            var credentials = UserCredentialsService.LoadCredentials();
            if (credentials == null)
            {
                MessageBox.Show(
                    "Brak loginu i has³a, proszê uzupe³niæ dane w ustawieniach.",
                    "EasyRCP - Brak loginu i has³a",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                Application.Run(new MainForm(isHidden: false));
                return;
            }

            // Check if the work has already started - runs in the background 6 times every 1 minute
            var result = await RcpAutomationService.CheckIfWorkAlreadyStartedWithRetryAsync();
            if (result == null)
            {
                MessageBox.Show(
                    "Brak po³¹czenia z internetem. Aplikacja dzia³a w tle i bêdzie dostêpna po wznowieniu po³¹czenia z internetem.",
                    "EasyRCP - Brak Internetu",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Application.Run(new MainForm(isHidden: true)); // running the hidden main form
                return;
            }
            else if (result == false)
            {
                using var prompt = new StartWorkPromptForm();
                if (prompt.ShowDialog() == DialogResult.Yes)
                {
                    await RcpAutomationService.StartWorkAsync();
                }
            }

            Application.Run(new MainForm(isHidden: true)); // running the hidden main form
        }
        catch (Exception ex)
        {
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
