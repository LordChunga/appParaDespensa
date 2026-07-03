#nullable enable

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PosLocal.Behaviors;

public static class MouseDoubleClickCommandBehavior
{
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
            "Command",
            typeof(ICommand),
            typeof(MouseDoubleClickCommandBehavior),
            new PropertyMetadata(null, OnCommandChanged));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.RegisterAttached(
            "CommandParameter",
            typeof(object),
            typeof(MouseDoubleClickCommandBehavior),
            new PropertyMetadata(null));

    public static void SetCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(CommandProperty, value);
    }

    public static ICommand? GetCommand(DependencyObject element)
    {
        return (ICommand?)element.GetValue(CommandProperty);
    }

    public static void SetCommandParameter(DependencyObject element, object? value)
    {
        element.SetValue(CommandParameterProperty, value);
    }

    public static object? GetCommandParameter(DependencyObject element)
    {
        return element.GetValue(CommandParameterProperty);
    }

    private static void OnCommandChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not Control control)
        {
            return;
        }

        if (e.OldValue is not null)
        {
            control.MouseDoubleClick -= OnMouseDoubleClick;
        }

        if (e.NewValue is not null)
        {
            control.MouseDoubleClick += OnMouseDoubleClick;
        }
    }

    private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DependencyObject dependencyObject)
        {
            return;
        }

        var command = GetCommand(dependencyObject);
        var parameter = GetCommandParameter(dependencyObject);

        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
            e.Handled = true;
        }
    }
}
