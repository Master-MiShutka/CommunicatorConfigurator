namespace TMP.Work.CommunicatorPSDTU.Common.Localization;

using System.Resources;
using System.Windows;

public sealed class Translation : DependencyObject
{
    public static readonly DependencyProperty ResourceManagerProperty =
        DependencyProperty.RegisterAttached("ResourceManager", typeof(ResourceManager), typeof(Translation));

    public static ResourceManager GetResourceManager(DependencyObject dependencyObject)
    {
        return (ResourceManager)dependencyObject.GetValue(ResourceManagerProperty);
    }

    public static void SetResourceManager(DependencyObject dependencyObject, ResourceManager value)
    {
        dependencyObject.SetValue(ResourceManagerProperty, value);
    }
}
