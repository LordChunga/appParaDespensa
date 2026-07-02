#nullable enable

using CommunityToolkit.Mvvm.ComponentModel;

namespace PosLocal.ViewModels;

public sealed partial class SettingToggleItemViewModel : ObservableObject
{
    private readonly Action<bool> _onChanged;

    public SettingToggleItemViewModel(
        string title,
        string description,
        string icon,
        bool isActive,
        Action<bool> onChanged)
    {
        Title = title;
        Description = description;
        Icon = icon;
        _isActive = isActive;
        _onChanged = onChanged;
    }

    public string Title { get; }

    public string Description { get; }

    public string Icon { get; }

    [ObservableProperty]
    private bool _isActive;

    partial void OnIsActiveChanged(bool value)
    {
        _onChanged(value);
    }
}
