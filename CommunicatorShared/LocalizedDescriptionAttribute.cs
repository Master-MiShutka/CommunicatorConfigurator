namespace TMP.Work.CommunicatorPSDTU.Common;

using System;
using System.ComponentModel;
using System.Resources;

#pragma warning disable IDE1006 // Naming Styles
public sealed class LocalizedDescriptionAttribute(string ResourceKey, Type ResourceType) : DescriptionAttribute
#pragma warning restore IDE1006 // Naming Styles
{
    private readonly ResourceManager resourceManager = new(ResourceType);

    public override string Description
    {
        get
        {
            string? description = this.resourceManager?.GetString(ResourceKey, System.Globalization.CultureInfo.CurrentCulture);

            return string.IsNullOrWhiteSpace(description) ? string.Format(System.Globalization.CultureInfo.CurrentCulture, "[[{0}]]", ResourceKey) : description;
        }
    }
}
