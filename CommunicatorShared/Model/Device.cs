namespace TMP.Work.CommunicatorPSDTU.Common.Model;

using System.ComponentModel.DataAnnotations;

public sealed partial class Device : ObservableObject
{
    [ObservableProperty] public partial string Status { get; set; } = string.Empty;

    [Display(Name = "NameProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial string Name { get; set; } = "<?>";

    [Display(Name = "IpAddressProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial string IpAddress { get; set; } = string.Empty;

    [Display(Name = "DeviceNameProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial string DeviceName { get; set; } = string.Empty;

    [Display(Name = "NetLevelProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial byte NetLevel { get; set; }

    [Display(Name = "NetModeProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial string NetMode { get; set; } = string.Empty;

    [Display(Name = "DeviceFirmwareVersionProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial string DeviceFirmwareVersion { get; set; } = string.Empty;

    [Display(Name = "DeviceFirmwareDateProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial string DeviceFirmwareDate { get; set; } = string.Empty;

    [Display(Name = "NumberOfConnectionAttempsProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial byte NumberOfConnectionAttemps { get; set; }

    [Display(Name = "DeviceNetworkTypeProperty", ResourceType = typeof(Resources.PropertiesNames))]
    [ObservableProperty] public partial string DeviceNetworkType { get; set; } = string.Empty;

    public override string ToString() => $"[{this.Name}] - '{this.DeviceName}' - {this.IpAddress} : 📶 {this.NetLevel}";
}
