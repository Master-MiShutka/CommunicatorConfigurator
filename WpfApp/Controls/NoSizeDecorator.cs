namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Controls;

using System.Windows;
using System.Windows.Controls;

public class NoSizeDecorator : Decorator
{
    protected override Size MeasureOverride(Size constraint)
    {
        // Ask for no space
        this.Child.Measure(new Size(0, 0));
        return new Size(0, 0);
    }
}
