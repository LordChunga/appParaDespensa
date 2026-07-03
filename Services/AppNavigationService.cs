#nullable enable

using System.Windows;

namespace PosLocal.Services;

public sealed class AppNavigationService : IAppNavigationService
{
    public void NavigateTo<TWindow>()
        where TWindow : Window
    {
        if (Application.Current is not App app)
        {
            throw new InvalidOperationException("La aplicacion WPF no esta inicializada.");
        }

        var currentWindow = Application.Current.MainWindow;
        if (currentWindow is null)
        {
            throw new InvalidOperationException("No hay una ventana principal activa para navegar.");
        }

        app.NavigateTo<TWindow>(currentWindow);
    }
}
