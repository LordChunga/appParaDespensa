#nullable enable

using System.Windows;
using System.Windows.Input;

namespace PosLocal.Behaviors;

public static class LoadedCommandBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(LoadedCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static void SetCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(CommandProperty, value);
    }

    public static ICommand? GetCommand(DependencyObject element)
    {
        return (ICommand?)element.GetValue(CommandProperty);
    }

    private static void OnCommandChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not FrameworkElement element)
        {
            return;
        }

        if (e.OldValue is not null)
        {
            element.Loaded -= OnLoaded;
        }

        if (e.NewValue is not null)
        {
            element.Loaded += OnLoaded;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not DependencyObject dependencyObject)
        {
            return;
        }

        var command = GetCommand(dependencyObject);
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }
}
