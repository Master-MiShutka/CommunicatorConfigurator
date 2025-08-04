namespace TMP.Work.CommunicatorPSDTU.Common.Resources;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

/// <summary>
/// Wraps up XAML access to instance of <see cref="Resources.UI_strings"/>, list of available cultures, and method to change culture
/// </summary>
public sealed class CultureResources
{
    //only fetch installed cultures once
    private static readonly bool FoundInstalledCultures;

    static CultureResources()
    {
        if (!FoundInstalledCultures)
        {
            //determine which cultures are available to this application
            Debug.WriteLine("Get Installed cultures:");

            CultureInfo cultureInfo = new CultureInfo("en");

            SupportedCultures.Add(cultureInfo);


            // startupPath
            FoundInstalledCulturesInDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

            // startupPath + libraries\locales - Beauty library moved locales there
            FoundInstalledCulturesInDirectory(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "libraries\\locales"));

            FoundInstalledCultures = true;
        }
    }

    private static void FoundInstalledCulturesInDirectory(string path)
    {
        if (System.IO.Path.Exists(path))
        {
            string executableName = System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);

            string localeLibName = executableName + ".resources.dll";

            foreach (string directory in System.IO.Directory.GetDirectories(path))
            {
                try
                {
                    //see if this directory corresponds to a valid culture name
                    System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(directory);

                    CultureInfo cultureInfo = CultureInfo.GetCultureInfo(directoryInfo.Name);

                    //determine if a resources dll exists in this directory that matches the executable name
                    if (directoryInfo.GetFiles(localeLibName).Length > 0)
                    {
                        SupportedCultures.Add(cultureInfo);
                        Debug.WriteLine($"Found Culture: {cultureInfo.DisplayName} [{cultureInfo.Name}]");
                    }
                }
                catch (ArgumentException) //ignore exceptions generated for any unrelated directories in the bin folder
                {
                }
            }
        }
    }

    /// <summary>
    /// List of available cultures, enumerated at startup
    /// </summary>
    public static List<CultureInfo> SupportedCultures { get; } = [];

    /// <summary>
    /// The Resources ObjectDataProvider uses this method to get an instance of the <see cref="Resources.UI_strings"/> class
    /// </summary>
    /// <returns></returns>
    public Resources.UI_strings GetResourceUIStringsInstance() => new();

    public static ObjectDataProvider UiStringsResourceProvider
    {
        get
        {
            field ??= (ObjectDataProvider)System.Windows.Application.Current.FindResource("LocUiStrings");

            return field;
        }
    }

    public static void ChangeCulture(CultureInfo culture)
    {
        //remain on the current culture if the desired culture cannot be found
        // - otherwise it would revert to the default resources set, which may or may not be desired.
        if (SupportedCultures.Contains(culture))
        {
            Resources.UI_strings.Culture = culture;
            Resources.PropertiesNames.Culture = culture;
            Resources.Strings.Culture = culture;
            Resources.ValidatingErrors.Culture = culture;
            Resources.NetErrors.Culture = culture;

            UiStringsResourceProvider.Refresh();

            Localization.TranslationSource.Instance.CurrentCulture = culture;
        }
        else
        {
            Debug.WriteLine($"Culture [{culture}] not available");
        }
    }
}
