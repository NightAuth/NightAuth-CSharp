using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NightAuthWpf;

public partial class App : Application
{
    protected override async void OnExit(ExitEventArgs e)
    {
        await AuthWindow.NightAuthApp.DisposeAsync();
        base.OnExit(e);
    }
}

public sealed class EmptyStringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class PlaceholderVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return Visibility.Visible;
        bool isFocused = values[0] is bool b && b;
        string? text = values[1] as string;
        bool hasText = !string.IsNullOrEmpty(text);
        return (!isFocused && !hasText) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
