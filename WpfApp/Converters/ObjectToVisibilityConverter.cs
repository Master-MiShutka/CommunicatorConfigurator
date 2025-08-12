namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

public class ObjectToVisibilityConverter : IValueConverter
{
    public bool IsNegative { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool condition = value is bool boolValue ? !boolValue : value == null;

        if (this.IsNegative)
        {
            condition = !condition;
        }

        return condition ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
