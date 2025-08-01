namespace TMP.Work.CommunicatorPSDTU.Common.Localization;

using System.Resources;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Xaml;

public sealed class LocExtension(string resourceKey) : MarkupExtension
{
    public string ResourceKey { get; init; } = resourceKey;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // targetObject is the control that is using the LocExtension
        object? targetObject = (serviceProvider as IProvideValueTarget)?.TargetObject;

        if (targetObject?.GetType().Name == "SharedDp") // is extension used in a control template?
        {
            return targetObject; // required for template re-binding
        }

        if (targetObject is null)
        {
            return DependencyProperty.UnsetValue;
        }

        string baseName = this.GetResourceManager(targetObject)?.BaseName ?? string.Empty;

        if (string.IsNullOrEmpty(baseName))
        {
            // rootObject is the root control of the visual tree (the top parent of targetObject)
            object? rootObject = (serviceProvider as IRootObjectProvider)?.RootObject;
            baseName = this.GetResourceManager(rootObject)?.BaseName ?? string.Empty;
        }

        if (string.IsNullOrEmpty(baseName)) // template re-binding
        {
            if (targetObject is FrameworkElement frameworkElement)
            {
                baseName = this.GetResourceManager(frameworkElement.TemplatedParent)?.BaseName ?? string.Empty;
            }
        }

        Binding binding = new Binding
        {
            Mode = BindingMode.OneWay,
            Path = new PropertyPath($"[{baseName}.{this.ResourceKey}]"),
            Source = TranslationSource.Instance,
            FallbackValue = this.ResourceKey,
            NotifyOnTargetUpdated = true,
        };

        return binding.ProvideValue(serviceProvider);
    }

    private ResourceManager? GetResourceManager(object? control)
    {
        if (control is DependencyObject dependencyObject)
        {
            object localValue = dependencyObject.ReadLocalValue(Translation.ResourceManagerProperty);

            // does this control have a "Translation.ResourceManager" attached property with a set value?
            if (localValue != DependencyProperty.UnsetValue)
            {
                if (localValue is ResourceManager resourceManager)
                {
                    TranslationSource.Instance.AddResourceManager(resourceManager);

                    return resourceManager;
                }
            }
        }

        return null;
    }
}
