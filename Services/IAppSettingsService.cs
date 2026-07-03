#nullable enable

namespace PosLocal.Services;

public interface IAppSettingsService
{
    AppSettings Settings { get; }

    string SettingsPath { get; }

    event EventHandler? SettingsChanged;

    void Save();
}
