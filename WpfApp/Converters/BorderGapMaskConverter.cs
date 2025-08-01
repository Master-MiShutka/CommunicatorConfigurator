namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Converters;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

public sealed class BorderGapMaskConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        //
        // Parameter Validation
        // 

        Type doubleType = typeof(double);

        if (parameter == null ||
            values == null ||
            values.Length != 3 ||
            values[0] == null ||
            values[1] == null ||
            values[2] == null ||
            !doubleType.IsAssignableFrom(values[0].GetType()) ||
            !doubleType.IsAssignableFrom(values[1].GetType()) ||
            !doubleType.IsAssignableFrom(values[2].GetType()))
        {
            return DependencyProperty.UnsetValue;
        }

        Type paramType = parameter.GetType();
        if (!(doubleType.IsAssignableFrom(paramType) || typeof(string).IsAssignableFrom(paramType)))
        {
            return DependencyProperty.UnsetValue;
        }

        // 
        // Conversion
        //

        double headerWidth = (double)values[0];
        double borderWidth = (double)values[1];
        double borderHeight = (double)values[2];

        // Doesn't make sense to have a Grid
        // with 0 as width or height 
        if (borderWidth == 0
            || borderHeight == 0)
        {
            return null;
        }

        // Width of the line to the left of the header 
        // to be used to set the width of the first column of the Grid
        double lineWidth;
        if (parameter is string parameterAsString)
        {
            lineWidth = double.Parse(parameterAsString, NumberFormatInfo.InvariantInfo);
        }
        else
        {
            lineWidth = (double)parameter;
        }

        Grid grid = new Grid
        {
            Width = borderWidth,
            Height = borderHeight
        };
        ColumnDefinition colDef1 = new ColumnDefinition();
        ColumnDefinition colDef2 = new ColumnDefinition();
        ColumnDefinition colDef3 = new ColumnDefinition();
        ColumnDefinition colDef4 = new ColumnDefinition();
        ColumnDefinition colDef5 = new ColumnDefinition();

        colDef1.Width = new GridLength(lineWidth);
        colDef2.Width = new GridLength(1, GridUnitType.Star);
        colDef3.Width = new GridLength(headerWidth);
        colDef4.Width = new GridLength(1, GridUnitType.Star);
        colDef5.Width = new GridLength(lineWidth);

        grid.ColumnDefinitions.Add(colDef1);
        grid.ColumnDefinitions.Add(colDef2);
        grid.ColumnDefinitions.Add(colDef3);
        grid.ColumnDefinitions.Add(colDef4);
        grid.ColumnDefinitions.Add(colDef5);

        RowDefinition rowDef1 = new RowDefinition();
        RowDefinition rowDef2 = new RowDefinition();

        rowDef1.Height = new GridLength(borderHeight / 2);
        rowDef2.Height = new GridLength(1, GridUnitType.Star);

        grid.RowDefinitions.Add(rowDef1);
        grid.RowDefinitions.Add(rowDef2);

        Rectangle rectColumn1 = new Rectangle();
        Rectangle rectColumn2 = new Rectangle();
        Rectangle rectColumn3 = new Rectangle();

        rectColumn1.Fill = Brushes.Black;
        rectColumn2.Fill = Brushes.Black;
        rectColumn3.Fill = Brushes.Black;

        Grid.SetRow(rectColumn1, 0);
        Grid.SetColumn(rectColumn1, 0);
        Grid.SetRowSpan(rectColumn1, 2);
        Grid.SetColumnSpan(rectColumn1, 2);

        Grid.SetRow(rectColumn2, 1);
        Grid.SetColumn(rectColumn2, 2);

        Grid.SetRow(rectColumn3, 0);
        Grid.SetColumn(rectColumn3, 3);
        Grid.SetRowSpan(rectColumn3, 2);
        Grid.SetColumnSpan(rectColumn3, 2);

        grid.Children.Add(rectColumn1);
        grid.Children.Add(rectColumn2);
        grid.Children.Add(rectColumn3);

        return new VisualBrush(grid);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        return [Binding.DoNothing];
    }
}
