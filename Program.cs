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
    static void Main()
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
                "Brak loginu i has³a",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        if (credentials != null && !RcpAutomationService.CheckIfWorkAlreadyStarted())
        {
            using var prompt = new StartWorkPromptForm();
            if (prompt.ShowDialog() == DialogResult.Yes)
            {
                RcpAutomationService.StartWork();
            }
        }

        Application.Run(new MainForm()); // running the hidden main form
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
