using System.Text.Json;

namespace EclatPlus;

internal sealed class AppSettings
{
    public int Eclat { get; set; }
    public int Contraste { get; set; }
    public bool DemarrerAvecWindows { get; set; }
    public bool AppliquerAuDemarrage { get; set; } = true;
    public bool LimiterAuPilote { get; set; }

    private static string Folder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EclatPlus");

    private static string FilePath => Path.Combine(Folder, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // Fichier corrompu : on repart sur les valeurs par défaut.
        }

        return new AppSettings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Folder);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, json);
    }
}
