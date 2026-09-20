using System.Globalization;

namespace WorkGrid.App.Converters;

public sealed class ActiveStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? Color.FromArgb("#2E7D32") : Color.FromArgb("#757575"); // Dark Green / Slate Grey
        }
        return Color.FromArgb("#000000");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
