using System.Text.Json;

namespace Caladabra.Desktop.Core;

public sealed class GameSettings
{
    public uint ScreenWidth { get; set; } = 1920;
    public uint ScreenHeight { get; set; } = 1080;
    public bool Fullscreen { get; set; } = false;
    public string Locale { get; set; } = "pl";
    public float MasterVolume { get; set; } = 1.0f;

    private const string SettingsFile = "settings.json";

    public static GameSettings Load()
    {
        if (File.Exists(SettingsFile))
        {
            try
            {
                var json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<GameSettings>(json) ?? new GameSettings();
            }
            catch
            {
                return new GameSettings();
            }
        }
        return new GameSettings();
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, options));
    }
}
