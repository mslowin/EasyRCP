using System.Diagnostics;
using System.Reflection;

namespace EasyRCP.Services
{
    public static class SelfRelocationService
    {
        public static void EnsureRunningFromCorrectLocation()
        {
            string? currentExePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(currentExePath))
            {
                throw new InvalidOperationException("Nie można ustalić ścieżki do bieżącego pliku wykonywalnego.");
            }

            string currentFolder = Path.GetDirectoryName(currentExePath)!;
            string userName = Environment.UserName;
            string targetFolder = $@"C:\Users\{userName}\EasyRCP";
            string exeFileName = Path.GetFileName(currentExePath);
            string targetExePath = Path.Combine(targetFolder, exeFileName);

            if (currentFolder.Equals(targetFolder, StringComparison.OrdinalIgnoreCase))
            {
                // App is already in the correct folder
                return;
            }

            if (!Directory.Exists($@"C:\Users\{userName}"))
            {
                // Folder doesn't exist – skipping relocation
                return;
            }

            MessageBox.Show(
                $"Aplikacja zostanie przeniesiona do stałego folderu {targetFolder} i uruchomiona ponownie",
                "EasyRCP - Przeniesienie do stałego folderu",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            string moveScriptPath = Path.Combine(currentFolder, "moveEasyRCP.bat");

            string script = $"""
                mkdir "{targetFolder}"
                taskkill /f /im EasyRCP.exe
                timeout /t 2 /nobreak > NUL
                move "{currentExePath}" "{targetExePath}"
                timeout /t 1 /nobreak > NUL
                start "" "{targetExePath}"
                timeout /t 2 /nobreak > NUL
                del "%~f0"
                """;

            File.WriteAllText(moveScriptPath, script);

            Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{moveScriptPath}\"")
            {
                CreateNoWindow = true
            });

            // Exiting current app instance just in case
            Environment.Exit(0);
        }
    }
}
