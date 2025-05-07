using System.Security.Cryptography;
using System.Text;

namespace EasyRCP.Services;

/// <summary>
/// Provides functionality to securely save and load user credentials.
/// </summary>
public static class UserCredentialsService
{
    /// <summary>
    /// The file path where the credentials are stored. Should be in C:\Users\USERNAME\AppData\Roaming\EasyRCP
    /// </summary>
    private static readonly string credentialsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "EasyRCP",
        "credentials.dat"
    );

    /// <summary>
    /// Saves the user's email and password securely to a file.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The user's password.</param>
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

    /// <summary>
    /// Loads the user's email and password from the secure file.
    /// </summary>
    /// <returns>
    /// A tuple containing the email and password if the credentials file exists and is valid; otherwise, <c>null</c>.
    /// </returns>
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
