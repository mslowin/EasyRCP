using System.Security.Cryptography;
using System.Text;

namespace EasyRCP.Services;

public static class UserCredentialsService
{
    private static readonly string credentialsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EasyRCP",
        "credentials.dat"
    );

    public static void SaveCredentials(string email, string password)
    {
        var directoryPath = Path.GetDirectoryName(credentialsFilePath);
        if (directoryPath != null)
        {
            Directory.CreateDirectory(directoryPath);
        }

        var combined = $"{email}|{password}";
        var data = Encoding.UTF8.GetBytes(combined);
        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(credentialsFilePath, encrypted);
    }

    public static (string Email, string Password)? LoadCredentials()
    {
        if (!File.Exists(credentialsFilePath))
            return null;

        var encrypted = File.ReadAllBytes(credentialsFilePath);
        var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
        var combined = Encoding.UTF8.GetString(decrypted);
        var parts = combined.Split('|');
        if (parts.Length == 2)
            return (parts[0], parts[1]);

        return null;
    }
}
