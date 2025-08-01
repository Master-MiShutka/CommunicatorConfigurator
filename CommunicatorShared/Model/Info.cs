namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;

    /// <summary>
    /// Описание устройства
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public partial class Info : ObservableValidator
    {
        /// <summary>
        /// Тип устройства
        /// </summary>
        [MaxLength(100, ErrorMessageResourceName = "MaxLengthHasBeenExceeded100", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [Display(Name = "DeviceNameProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial string Name { get; set; } = string.Empty;

        /// <summary>
        /// Порядковый номер прошивки
        /// </summary>
        [MaxLength(6, ErrorMessageResourceName = "MaxLengthHasBeenExceeded6", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [Display(Name = "DeviceFirmwareVersionProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial string FirmwareVersion { get; set; } = string.Empty;

        /// <summary>
        /// Дата последнего внесения изменений в прошивку
        /// </summary>
        [MaxLength(6, ErrorMessageResourceName = "MaxLengthHasBeenExceeded6", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [Display(Name = "DeviceFirmwareDateProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial string FirmwareDate { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{this.Name}, версия {this.FirmwareVersion} от {this.FirmwareDate}";
        }

        private string GetDebuggerDisplay() => this.ToString();
    }
}
