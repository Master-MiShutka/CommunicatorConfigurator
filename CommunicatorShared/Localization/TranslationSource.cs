namespace TMP.Work.CommunicatorPSDTU.Common.Localization;

using System.Globalization;
using System.Resources;

public partial class TranslationSource : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    private readonly Dictionary<string, ResourceManager> resourceManagerDictionary = [];
    private readonly ResourceManager resManager = Resources.Strings.ResourceManager;

    public static TranslationSource Instance { get; } = new TranslationSource();

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    public partial CultureInfo CurrentCulture { get; set; } = CultureInfo.InstalledUICulture;

    public string? this[string key]
    {
        get
        {
            var (baseName, stringName) = SplitName(key);
            string? translation = null;
            if (this.resourceManagerDictionary.TryGetValue(baseName, out var value))
            {
                translation = value.GetString(stringName, this.CurrentCulture);
            }

            return translation ?? key;
        }
    }

    public void AddResourceManager(ResourceManager resourceManager)
    {
        this.resourceManagerDictionary.TryAdd(resourceManager.BaseName, resourceManager);
    }

    partial void OnCurrentCultureChanged(CultureInfo value)
    {
        Resources.CultureResources.ChangeCulture(value);
    }

    public static (string baseName, string stringName) SplitName(string name)
    {
        int idx = name.LastIndexOf('.');
        return (name[..idx], name[(idx + 1)..]);
    }
}
