#nullable enable

using System.Windows;

namespace PosLocal.Behaviors;

public static class CloseWindowOnTrueBehavior
{
    public static readonly DependencyProperty CloseWhenTrueProperty =
        DependencyProperty.RegisterAttached(
            "CloseWhenTrue",
            typeof(bool),
            typeof(CloseWindowOnTrueBehavior),
            new PropertyMetadata(false, OnCloseWhenTrueChanged));

    public static void SetCloseWhenTrue(DependencyObject element, bool value)
    {
        element.SetValue(CloseWhenTrueProperty, value);
    }

    public static bool GetCloseWhenTrue(DependencyObject element)
    {
        return (bool)element.GetValue(CloseWhenTrueProperty);
    }

    private static void OnCloseWhenTrueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is not true)
        {
            return;
        }

        var window = dependencyObject as Window
            ?? Window.GetWindow(dependencyObject);

        window?.Close();
    }
}
