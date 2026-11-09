using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace InsertionSort.Converters;

/// <summary>
/// Converts a Boolean value to Visible or Collapsed.
/// </summary>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is true ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is Visibility.Visible;
    }
}
