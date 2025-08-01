namespace TMP.Work.CommunicatorPSDTU.UI.Wpf.Behaviors;

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;

internal sealed class RestrictStringInputBehavior : Behavior<TextBox>
{
    protected override void OnAttached()
    {
        this.AssociatedObject.Loaded += (sender, args) => this.SetMaxLength();
        base.OnAttached();
    }

    private void SetMaxLength()
    {
        object? context = this.AssociatedObject.DataContext;
        BindingExpression? binding = this.AssociatedObject.GetBindingExpression(TextBox.TextProperty);

        if (context != null && binding != null)
        {
            PropertyInfo? prop = context.GetType().GetProperty(binding.ParentBinding.Path.Path);
            if (prop != null)
            {
                if (prop.GetCustomAttributes(typeof(MaxLengthAttribute), true).FirstOrDefault() is MaxLengthAttribute att)
                {
                    this.AssociatedObject.MaxLength = att.Length;
                }
            }
        }
    }
}
