#nullable enable

using System.IO;
using System.Text.Json;

namespace PosLocal.Services;

public sealed class AppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public AppSettingsService(string settingsPath)
    {
        SettingsPath = settingsPath;
        Settings = LoadSettings(settingsPath);
        Save();
    }

    public AppSettings Settings { get; }

    public string SettingsPath { get; }

    public event EventHandler? SettingsChanged;

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(Settings, SerializerOptions);
        File.WriteAllText(SettingsPath, json);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static AppSettings LoadSettings(string settingsPath)
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions)
                ?? new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
    }
}
