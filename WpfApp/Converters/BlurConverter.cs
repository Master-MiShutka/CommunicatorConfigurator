using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Effects;

namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Converters
{
    public sealed class BlurConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isBusy && isBusy)
            {
                return new BlurEffect() { Radius = 10d };
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
