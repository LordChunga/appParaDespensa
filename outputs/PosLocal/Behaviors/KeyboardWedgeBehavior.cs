#nullable enable

using System.Text;
using System.Windows;
using System.Windows.Input;

namespace PosLocal.Behaviors;

public static class KeyboardWedgeBehavior
{
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(KeyboardWedgeBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty ScanCompletedCommandProperty =
        DependencyProperty.RegisterAttached(
            "ScanCompletedCommand",
            typeof(ICommand),
            typeof(KeyboardWedgeBehavior),
            new PropertyMetadata(null));

    public static readonly DependencyProperty MinimumLengthProperty =
        DependencyProperty.RegisterAttached(
            "MinimumLength",
            typeof(int),
            typeof(KeyboardWedgeBehavior),
            new PropertyMetadata(4));

    public static readonly DependencyProperty InterKeyTimeoutMillisecondsProperty =
        DependencyProperty.RegisterAttached(
            "InterKeyTimeoutMilliseconds",
            typeof(int),
            typeof(KeyboardWedgeBehavior),
            new PropertyMetadata(80));

    private static readonly DependencyProperty ScannerStateProperty =
        DependencyProperty.RegisterAttached(
            "ScannerState",
            typeof(ScannerState),
            typeof(KeyboardWedgeBehavior),
            new PropertyMetadata(null));

    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    public static bool GetIsEnabled(DependencyObject element)
    {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    public static void SetScanCompletedCommand(DependencyObject element, ICommand? value)
    {
        element.SetValue(ScanCompletedCommandProperty, value);
    }

    public static ICommand? GetScanCompletedCommand(DependencyObject element)
    {
        return (ICommand?)element.GetValue(ScanCompletedCommandProperty);
    }

    public static void SetMinimumLength(DependencyObject element, int value)
    {
        element.SetValue(MinimumLengthProperty, value);
    }

    public static int GetMinimumLength(DependencyObject element)
    {
        return (int)element.GetValue(MinimumLengthProperty);
    }

    public static void SetInterKeyTimeoutMilliseconds(DependencyObject element, int value)
    {
        element.SetValue(InterKeyTimeoutMillisecondsProperty, value);
    }

    public static int GetInterKeyTimeoutMilliseconds(DependencyObject element)
    {
        return (int)element.GetValue(InterKeyTimeoutMillisecondsProperty);
    }

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not UIElement element)
        {
            return;
        }

        if ((bool)e.OldValue)
        {
            element.PreviewKeyDown -= OnPreviewKeyDown;
        }

        if ((bool)e.NewValue)
        {
            element.SetValue(ScannerStateProperty, new ScannerState());
            element.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not DependencyObject dependencyObject)
        {
            return;
        }

        var state = (ScannerState?)dependencyObject.GetValue(ScannerStateProperty);
        if (state is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var timeout = TimeSpan.FromMilliseconds(GetInterKeyTimeoutMilliseconds(dependencyObject));
        if (now - state.LastKeyUtc > timeout)
        {
            state.Buffer.Clear();
        }

        state.LastKeyUtc = now;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Enter)
        {
            var scan = state.Buffer.ToString();
            state.Buffer.Clear();

            if (scan.Length < GetMinimumLength(dependencyObject))
            {
                return;
            }

            var command = GetScanCompletedCommand(dependencyObject);
            if (command?.CanExecute(scan) == true)
            {
                command.Execute(scan);
                e.Handled = true;
            }

            return;
        }

        var character = TryGetBarcodeCharacter(key);
        if (character is null)
        {
            return;
        }

        state.Buffer.Append(character.Value);
    }

    private static char? TryGetBarcodeCharacter(Key key)
    {
        if (key >= Key.D0 && key <= Key.D9)
        {
            return (char)('0' + ((int)key - (int)Key.D0));
        }

        if (key >= Key.NumPad0 && key <= Key.NumPad9)
        {
            return (char)('0' + ((int)key - (int)Key.NumPad0));
        }

        if (key >= Key.A && key <= Key.Z)
        {
            return (char)('A' + ((int)key - (int)Key.A));
        }

        return key switch
        {
            Key.OemMinus or Key.Subtract => '-',
            Key.OemPlus or Key.Add => '+',
            _ => null
        };
    }

    private sealed class ScannerState
    {
        public StringBuilder Buffer { get; } = new();

        public DateTime LastKeyUtc { get; set; } = DateTime.MinValue;
    }
}
