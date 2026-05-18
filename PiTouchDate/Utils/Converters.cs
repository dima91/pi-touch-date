namespace PiTouchDate.Utils.Converters;

using System;
using System.Globalization;
using Avalonia.Data.Converters;

public class NullToDoubleDashConverter : IValueConverter
{
    public static readonly NullToDoubleDashConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is null ? "--" : value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is "--" ? null : value;
}
