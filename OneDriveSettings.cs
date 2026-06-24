using System.Text.Json;

namespace QRGenerator;

public class OneDriveSettings
{
    public string TenantId            { get; set; } = "";
    public string ClientId            { get; set; } = "";
    public string ClientSecret        { get; set; } = "";
    public string UserEmail           { get; set; } = "";
    public string TargetFolder { get; set; } = "";

    // Environment.ProcessPath gives the real exe location even in single-file publish,
    // unlike AppDomain.CurrentDomain.BaseDirectory which resolves to the temp extraction folder.
    private static readonly string SettingsPath = Path.Combine(
        Path.GetDirectoryName(Environment.ProcessPath)
        ?? AppDomain.CurrentDomain.BaseDirectory,
        "onedrive-settings.json");

    public static OneDriveSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<OneDriveSettings>(File.ReadAllText(SettingsPath))
                       ?? new OneDriveSettings();
        }
        catch { }
        return new OneDriveSettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(SettingsPath,
                JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
