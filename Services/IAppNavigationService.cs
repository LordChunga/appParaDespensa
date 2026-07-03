#nullable enable

using System.Windows;

namespace PosLocal.Services;

public interface IAppNavigationService
{
    void NavigateTo<TWindow>()
        where TWindow : Window;
}
