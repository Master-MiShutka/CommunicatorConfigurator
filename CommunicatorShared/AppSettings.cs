namespace TMP.Work.CommunicatorPSDTU.Common;

using System.ComponentModel;
using System.Globalization;
using PropertyChanged.SourceGenerator;
using TMP.Work.CommunicatorPSDTU.Common.Utils;

public partial class AppSettings : CommunityToolkit.Mvvm.ComponentModel.ObservableObject, IDataErrorInfo
{
    public const string FileName = "CommunicatorPSDTUAppSettings.json";

    private const string Falback_language = "en";

    [System.Text.Json.Serialization.JsonIgnore]
    [PropertyChanged.SourceGenerator.PropertyAttribute("System.Text.Json.Serialization.JsonIgnore")]
    [Notify] private bool hasError;

    [Notify] private bool appUseFluentTheme;
    [Notify] private string? appLanguage;

    [System.Text.Json.Serialization.JsonIgnore]
    [PropertyChanged.SourceGenerator.PropertyAttribute("System.Text.Json.Serialization.JsonIgnore")]
    [Notify] private CultureInfo? selectedCultureInfo;

    /// <summary>
    /// Ip адрес для подключения
    /// </summary>
    [Notify] private string ipAddress = string.Empty;

    /// <summary>
    /// Интервал проверки уровня сигнала, мс
    /// </summary>
    [Notify] private ushort netStrengthCheckInterval = 1_000;

    /// <summary>
    /// Пауза между попытками, мс
    /// </summary>
    [Notify] private ushort delayBetweenRetries = 1_000;

    /// <summary>
    /// Количество попыток
    /// </summary>
    [Notify] private ushort retryCount = 3;

    /// <summary>
    /// Таймаут ожидания ответа, секунд
    /// </summary>
    [Notify] private ushort sendOrReceiveTimeout = 30;

    /// <summary>
    /// Таймаут ожидания соединения, секунд
    /// </summary>
    [Notify] private ushort connectionWaitTimeout = 30;

    private void OnAppUseFluentThemeChanged()
    {
#pragma warning disable WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        System.Windows.Application.Current.ThemeMode = this.AppUseFluentTheme ? System.Windows.ThemeMode.System : System.Windows.ThemeMode.None;
#pragma warning restore WPF0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }

    private void OnSelectedCultureInfoChanged()
    {
        this.AppLanguage = this.SelectedCultureInfo?.Name ?? Falback_language;
    }

    private void OnAppLanguageChanged()
    {
        if (string.IsNullOrEmpty(this.AppLanguage))
        {
            this.AppLanguage = Falback_language;
        }

        var cultureInfo = new CultureInfo(this.AppLanguage);

        if (Resources.CultureResources.SupportedCultures.Contains(cultureInfo) == false)
        {
            this.AppLanguage = Falback_language;
            cultureInfo = new CultureInfo(this.AppLanguage);
        }

        try
        {
            TMP.Work.CommunicatorPSDTU.Common.Resources.CultureResources.ChangeCulture(cultureInfo);

            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            System.Threading.Thread.CurrentThread.CurrentUICulture = cultureInfo;
            System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;

            this.SelectedCultureInfo = cultureInfo;

            this.OnPropertyChanged(nameof(this.IpAddressValidationError));
        }
        catch
        {
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string Error { get; } = string.Empty;

    [System.Text.Json.Serialization.JsonIgnore]
    public string IpAddressValidationError => this[nameof(this.IpAddress)];

    public string this[string columnName]
    {
        get
        {
            string error = string.Empty;
            switch (columnName)
            {
                case nameof(this.IpAddress):
                    (bool isOk, string ipError) = IPAddressValidator.Validate(this.IpAddress);
                    if (isOk == false)
                    {
                        error = ipError; // Resources.Strings.DEFAULT_NO_IP_ADDRESS_MESSAGE
                    }
                    break;
                default:
                    //Обработка ошибок для свойства
                    break;
            }

            this.HasError = !string.IsNullOrEmpty(error);
            this.OnPropertyChanged(nameof(this.HasError));

            return error;
        }
    }
}
