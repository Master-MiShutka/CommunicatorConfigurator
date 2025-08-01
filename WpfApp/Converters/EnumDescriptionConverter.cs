using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        private string? GetEnumDescription(Enum enumObj)
        {
            FieldInfo? fieldInfo = enumObj.GetType().GetField(enumObj.ToString());

            if (fieldInfo == null)
            {
                return null;
            }

            object[] attribArray = fieldInfo.GetCustomAttributes(false);

            if (attribArray.Length == 0)
            {
                return enumObj.ToString();
            }
            else
            {
                DescriptionAttribute? attrib = attribArray[0] as DescriptionAttribute;
                return attrib?.Description;
            }
        }

        object? IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Enum myEnum = (Enum)value;
            string? description = this.GetEnumDescription(myEnum);
            return description;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
