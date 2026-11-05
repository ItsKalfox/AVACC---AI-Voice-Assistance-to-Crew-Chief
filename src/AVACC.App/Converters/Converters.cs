using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AVACC.Core.Models;

namespace AVACC.App.Converters;

public class StateToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ProcessingState state)
        {
            return state switch
            {
                ProcessingState.Idle => new SolidColorBrush(Color.FromRgb(85, 92, 110)),
                ProcessingState.ButtonPressed => new SolidColorBrush(Color.FromRgb(255, 171, 0)),
                ProcessingState.Recording => new SolidColorBrush(Color.FromRgb(255, 23, 68)),
                ProcessingState.ButtonReleased => new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                ProcessingState.ProcessingAudio => new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                ProcessingState.Transcribing => new SolidColorBrush(Color.FromRgb(179, 136, 255)),
                ProcessingState.InterpretingCommand => new SolidColorBrush(Color.FromRgb(0, 229, 255)),
                ProcessingState.Completed => new SolidColorBrush(Color.FromRgb(0, 230, 118)),
                ProcessingState.Error => new SolidColorBrush(Color.FromRgb(255, 23, 68)),
                _ => new SolidColorBrush(Color.FromRgb(85, 92, 110))
            };
        }

        return new SolidColorBrush(Color.FromRgb(85, 92, 110));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class LogLevelToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AVACC.Core.Models.LogLevel level)
        {
            return level switch
            {
                AVACC.Core.Models.LogLevel.Info => new SolidColorBrush(Color.FromRgb(142, 149, 165)),
                AVACC.Core.Models.LogLevel.Warning => new SolidColorBrush(Color.FromRgb(255, 171, 0)),
                AVACC.Core.Models.LogLevel.Error => new SolidColorBrush(Color.FromRgb(255, 23, 68)),
                AVACC.Core.Models.LogLevel.Debug => new SolidColorBrush(Color.FromRgb(85, 92, 110)),
                _ => new SolidColorBrush(Color.FromRgb(142, 149, 165))
            };
        }

        return new SolidColorBrush(Color.FromRgb(142, 149, 165));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class NullOrEmptyToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var isEmpty = value == null || (value is string s && string.IsNullOrWhiteSpace(s));
        if (Invert) isEmpty = !isEmpty;
        return isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class EqualityToBooleanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return false;
        return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b && parameter != null)
        {
            if (int.TryParse(parameter.ToString(), out var intVal))
            {
                return intVal;
            }
            return parameter;
        }
        return Binding.DoNothing;
    }
}

public class EqualityToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;
        var isEqual = string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        return isEqual ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToBrushConverter : IValueConverter
{
    public Brush TrueBrush { get; set; } = new SolidColorBrush(Color.FromRgb(255, 171, 0)); // Amber warning
    public Brush FalseBrush { get; set; } = new SolidColorBrush(Color.FromRgb(25, 28, 36));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? TrueBrush : FalseBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
