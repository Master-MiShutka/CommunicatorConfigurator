using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Converters
{
    public class EnumBindingConverter : MarkupExtension, IValueConverter
    {
        private sealed class Dictionaries
        {
            public Dictionary<object, string> Descriptions { get; } = [];
            public Dictionary<string, object> IntValues { get; } = [];
            public IEnumerable<string>? ItemsSource { get; set; }
        }

        private static readonly Dictionary<Type, Dictionaries> LocalDictionaries = [];

        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null || !value.GetType().IsEnum)
            {
                return value;
            }

            object? getValue(object v)
            {
                if (parameter == null)
                {
                    return v;
                }
                else
                {
                    try
                    {
                        string format = parameter.ToString() ?? string.Empty;
                        return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, v);
                    }
                    catch
                    {
                        return DependencyProperty.UnsetValue;
                    }
                }
            }

            if (!LocalDictionaries.ContainsKey(value.GetType()))
            {
                this.CreateDictionaries(value.GetType());
            }

            // This is for the ItemsSource
            if (targetType == typeof(System.Collections.IEnumerable))
            {
                return LocalDictionaries[value.GetType()].ItemsSource;
            }

            // Normal SelectedItem case where it exists
            if (LocalDictionaries[value.GetType()].Descriptions.TryGetValue(value, out var value1))
            {
                return getValue(value1);
            }

            // Have to handle 0 case, else an issue
            if ((int)value == 0)
            {
                return DependencyProperty.UnsetValue;
            }

            // Error condition
            throw new InvalidEnumArgumentException();
        }

        public object? ConvertBack(object? value, Type? targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string? valueAsString = (string?)value;

            if (value == null) // (valueAsString == null && targetType != null && targetType.IsEnum && !targetType.IsGenericType)
            {
                return value;
            }

            if (targetType != null)
            {
                if (targetType.IsGenericType)
                {
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                if (!LocalDictionaries.ContainsKey(targetType!))
                {
                    this.CreateDictionaries(targetType!);
                }

                valueAsString ??= string.Empty;

                if (LocalDictionaries[targetType!].IntValues.TryGetValue(valueAsString, out var enumInt) && targetType != null)
                {
                    return Enum.ToObject(targetType, enumInt);
                }

                return DependencyProperty.UnsetValue;
            }

            return DependencyProperty.UnsetValue;
        }

        private void CreateDictionaries(Type e)
        {
            var dictionaries = new Dictionaries();

            foreach (var value in Enum.GetValues(e))
            {
                if (value != null)
                {
                    string valueAsString = value.ToString() ?? "<?>";

                    FieldInfo? info = value.GetType().GetField(valueAsString);

                    if (info != null)
                    {
                        var valueDescription = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);

                        if (valueDescription != null && valueDescription.Length == 1)
                        {
                            dictionaries.Descriptions.Add(value, valueDescription[0].Description);
                            dictionaries.IntValues.Add(valueDescription[0].Description, value);
                        }
                        else
                        {
                            var label = (DescriptionAttribute[])info.GetCustomAttributes(typeof(DescriptionAttribute), false);

                            if (label != null && label.Length > 0)
                            {
                                string lbl = label[0].Description ?? valueAsString;

                                dictionaries.Descriptions.Add(value, lbl);
                                dictionaries.IntValues.Add(lbl, value);
                            }
                            else
                            {
                                // Use the value for display if not concrete result
                                dictionaries.Descriptions.Add(value, valueAsString);
                                dictionaries.IntValues.Add(valueAsString, value);
                            }
                        }
                    }
                }
            }

            dictionaries.ItemsSource = dictionaries.Descriptions.Select(i => i.Value);
            LocalDictionaries.Add(e, dictionaries);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
