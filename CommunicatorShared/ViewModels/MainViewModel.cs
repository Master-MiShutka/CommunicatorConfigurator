namespace TMP.Work.CommunicatorPSDTU.Common.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Microsoft.Extensions.Logging;
    using PropertyChanged.SourceGenerator;
    using TMP.Work.CommunicatorPSDTU.Common.Model;

    public sealed partial class MainViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject, IDisposable
    {
        private enum MainViewModelState
        {
            Ready,
            Connecting,
            Connected,
            Cancelling,
            CheckingDevice,
            CheckingDevices
        }

        private readonly ILogger logger;

        [Notify(get: Getter.Private, set: Setter.Private)] private MainViewModelState state;

        /// <summary>
        /// Флаг, указывающий на выполнение операции
        /// </summary>
        [Notify(set: Setter.Private)] private bool isBusy;

        /// <summary>
        /// Флаг, указывающий на состояние подключения
        /// </summary>
        [Notify(set: Setter.Private)] private bool isConnected;

        [Notify(set: Setter.Private)] private Model.Info? deviceInfo;
        [Notify(set: Setter.Private)] private Model.NetworkConfig? deviceNetworkConfig;
        [Notify(set: Setter.Private)] private byte deviceNetLevel;
        [Notify(set: Setter.Private)] private string deviceNetMode = string.Empty;
        [Notify(set: Setter.Private)] private Model.Config? deviceConfig;
        [Notify(set: Setter.Private)] private Model.SerialConfig? deviceRs485Config;

        [Notify(set: Setter.Private)] private Model.Config? newDeviceConfig;
        [Notify(set: Setter.Private)] private Model.SerialConfig? newRs485Config;


        [Notify(set: Setter.Private)] private INotifyPropertyChanged? dialog;

        [Notify(set: Setter.Private)] private string? checkingDevicesState;

        /// <summary>
        /// Список устройств для проверки
        /// </summary>
        [Notify(set: Setter.Private)]
        private ObservableCollection<Model.Device> devices = [];

        private System.Threading.Timer? netLevelCheckTimer;

        private CancellationTokenSource? cancellationTokenSource;

        private ConnectingViewModel? connectingViewModel;

        private CommunicatorTcpClient? communicatorTcpClient;

        private bool isExcelConfigured;

        [Notify(set: Setter.Private)] private bool isDevicesChecking, isDevicesChecked;

        private readonly Dispatcher uiDispatcher;

        private bool isCheckNetworkModeAndLevel;

        public MainViewModel(ILogger logger)
        {
            this.logger = logger;
            this.logger.LogTrace("MainViewModel ctor");

            this.uiDispatcher = System.Windows.Application.Current.Dispatcher;

            if (System.IO.File.Exists(AppSettings.FileName))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(AppSettings.FileName, System.Text.Encoding.UTF8);
                    this.Settings = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json) ?? new();
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, "Failed to read program settings.");
                }
            }

            this.Settings ??= new();

            this.Settings.PropertyChanged += this.Settings_PropertyChanged;

            CultureInfo cultureInfo = CultureInfo.CurrentUICulture;

            if (string.IsNullOrEmpty(this.Settings.AppLanguage) == false)
            {
                cultureInfo = new(this.Settings.AppLanguage);
            }

            if (Common.Resources.CultureResources.SupportedCultures.Contains(cultureInfo))
            {
                this.Settings.AppLanguage = cultureInfo.Name;
            }

            this.State = MainViewModelState.Ready;
        }

        ~MainViewModel() => this.Dispose();

        #region Private methods

        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSettings.AppLanguage))
            {
                this.OnPropertyChanged(nameof(this.Message));
                this.OnPropertyChanged(nameof(this.ConnectCommandHeader));
            }

            if (e.PropertyName == nameof(AppSettings.NetStrengthCheckInterval))
            {
                if (this.isCheckNetworkModeAndLevel)
                {
                    this.netLevelCheckTimer?.Change(dueTime: TimeSpan.Zero, period: TimeSpan.FromMilliseconds(this.Settings.NetStrengthCheckInterval));
                }
            }

            if (e.PropertyName == nameof(AppSettings.IpAddress))
            {
                this.ConnectCommand.NotifyCanExecuteChanged();
            }
        }

        private void OnStateChanged()
        {
            switch (this.state)
            {
                case MainViewModelState.Ready:
                    this.IsConnected = false;

                    this.netLevelCheckTimer?.Dispose();
                    this.communicatorTcpClient?.Close();

                    this.cancellationTokenSource?.TryReset();

                    this.CheckingDevicesState = null;

                    this.uiDispatcher.Invoke(this.ClearListCommand.NotifyCanExecuteChanged);
                    this.uiDispatcher.Invoke(this.StartCheckDevicesCommand.NotifyCanExecuteChanged);

                    this.uiDispatcher.Invoke(this.PasteListFromClipboardCommand.NotifyCanExecuteChanged);
                    this.uiDispatcher.Invoke(this.CopyResultToClipboardCommand.NotifyCanExecuteChanged);

                    this.uiDispatcher.Invoke(this.PasteListFromFileCommand.NotifyCanExecuteChanged);
                    this.uiDispatcher.Invoke(this.WriteResultToFileCommand.NotifyCanExecuteChanged);

                    break;

                case MainViewModelState.Connecting:

                    this.IsBusy = true;
                    this.OnPropertyChanged(nameof(this.IsReady));

                    this.IsConnected = false;

                    this.DeviceConfig = null;
                    this.DeviceRs485Config = null;
                    this.NewDeviceConfig = null;
                    this.NewRs485Config = null;
                    this.DeviceInfo = null;
                    this.DeviceNetLevel = 0;
                    this.DeviceNetMode = string.Empty;
                    this.DeviceNetworkConfig = null;

                    break;

                case MainViewModelState.Connected:
                    this.IsConnected = true;

                    this.cancellationTokenSource?.TryReset();

                    this.netLevelCheckTimer = new System.Threading.Timer(callback: this.NetLevelCheckTimerCallback,
                                                                                         null,
                                                                                         dueTime: TimeSpan.Zero,
                                                                                         period: TimeSpan.FromMilliseconds(this.Settings.NetStrengthCheckInterval));

                    break;

                case MainViewModelState.Cancelling:

                    this.cancellationTokenSource?.TryReset();

                    break;

                case MainViewModelState.CheckingDevice:
                    break;
                case MainViewModelState.CheckingDevices:

                    this.IsDevicesChecking = true;

                    this.uiDispatcher.Invoke(this.StartCheckDevicesCommand.NotifyCanExecuteChanged);
                    this.uiDispatcher.Invoke(this.PasteListFromClipboardCommand.NotifyCanExecuteChanged);
                    this.uiDispatcher.Invoke(this.ClearListCommand.NotifyCanExecuteChanged);

                    this.IsConnected = false;

                    this.netLevelCheckTimer?.Dispose();
                    this.communicatorTcpClient?.Close();

                    this.cancellationTokenSource?.TryReset();
                    break;

                default:
                    break;
            }

            this.OnPropertyChanged(nameof(this.ConnectCommandHeader));
            this.OnPropertyChanged(nameof(this.Message));

            this.uiDispatcher.Invoke(this.ConnectCommand.NotifyCanExecuteChanged);
        }

        private void OnDialogChanged()
        {
            if (this.dialog == null)
            {
                this.IsBusy = false;
                this.OnPropertyChanged(nameof(this.IsReady));
            }
            else
            {
                this.IsBusy = true;
                this.OnPropertyChanged(nameof(this.IsReady));
            }
        }

        private void OnNewDeviceConfigChanged(Model.Config? oldValue, Model.Config? newValue)
        {
            oldValue?.PropertyChanged -= this.NewDeviceConfig_PropertyChanged;

            newValue?.PropertyChanged += this.NewDeviceConfig_PropertyChanged;
        }

        private void OnNewRs485ConfigChanged(Model.SerialConfig? oldValue, Model.SerialConfig? newValue)
        {
            oldValue?.PropertyChanged -= this.NewRs485Config_PropertyChanged;

            newValue?.PropertyChanged += this.NewRs485Config_PropertyChanged;
        }

        private void NewDeviceConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.SaveDeviceConfigCommand.NotifyCanExecuteChanged();
        }

        private void NewRs485Config_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            this.SaveDeviceConfigCommand.NotifyCanExecuteChanged();
        }

        private void ShowDialog(INotifyPropertyChanged model)
        {
            this.Dialog = model;
        }

        private void ShowDialogMessage(string message, string? detailedMessage = null, Action? onClose = null)
        {
            this.Dialog = new MessageViewModel(message, detailedMessage, onClose: () =>
            {
                this.Dialog = null;
                onClose?.Invoke();
            });
        }

        private void NetLevelCheckTimerCallback(object? state)
        {
            if (this.isCheckNetworkModeAndLevel)
            {
                return;
            }

            if (this.communicatorTcpClient != null && this.communicatorTcpClient.Connected)
            {
                var oldState = this.State;

                this.State = MainViewModelState.CheckingDevice;

                _ = Task.Run(this.CheckNetworkModeAndLevelAsync)
                .ContinueWith(t =>
                {
                    this.State = oldState;
                    this.isCheckNetworkModeAndLevel = false;
                });
            }
            else
            {
                this.IsConnected = false;
                this.netLevelCheckTimer?.Dispose();
            }
        }

        private async Task CheckNetworkModeAndLevelAsync()
        {
            if (this.communicatorTcpClient == null)
            {
                return;
            }

            this.isCheckNetworkModeAndLevel = true;

            // Проверка уровня сигнала и типа связи
            var (networkMode, networkLevel) = await this.communicatorTcpClient.ReadNetworkModeAndLevelAsync();

            if (networkLevel > 0)
            {
                this.DeviceNetLevel = networkLevel;
                this.DeviceNetMode = networkMode;
            }
        }

        private async Task OnConnect(CommunicatorTcpClient client)
        {
            this.logger.LogTrace("MainViewModel : OnConnect");

            if (client.Connected)
            {
                string message = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Strings.SUCCESSFULLY_CONNECTED_TO, this.Settings.IpAddress, CommunicatorTcpClient.SETUP_PORT);

                this.logger.LogDebug(message);
                this.connectingViewModel?.DetailMessage = message;

                try
                {
                    #region чтение конфигурации

                    // чтение информации об устройстве
                    this.DeviceInfo = await client.ReadInfoAsync();

                    if (string.IsNullOrEmpty(this.DeviceInfo.Name))
                    {
                        this.Dialog = null;

                        this.State = MainViewModelState.Ready;

                        this.ShowDialogMessage(Resources.Strings.InvalidAnswerUnknownDevice);

                        return;
                    }

                    // чтение конфигурации сети
                    this.DeviceNetworkConfig = await client.ReadNetworkConfigAsync();

                    await this.CheckNetworkModeAndLevelAsync();

                    // чтение конфигурации устройства
                    var config = await client.ReadConfig();

                    if (config.DeviceConfig != null && config.SerialConfig != null)
                    {

                        this.DeviceConfig = config.DeviceConfig;
                        this.DeviceRs485Config = config.SerialConfig;

                        this.NewDeviceConfig = this.DeviceConfig.CloneConfig();
                        this.NewRs485Config = this.DeviceRs485Config.CloneConfig();

                    }
                    else
                    {
                        this.logger.LogTrace("config.DeviceConfig == null || config.SerialConfig == null");
                    }

                    #endregion

                    this.State = MainViewModelState.Connected;
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, Resources.Strings.ERROR_DATA_READING);

                    var exDetails = TMP.Shared.Common.Utils.GetExceptionDetails(e);

                    this.logger.LogError(exDetails);

                    this.State = MainViewModelState.Ready;

                    this.ShowDialogMessage(Resources.Strings.FAILED_TO_ESTABLISH_CONNECTION);
                }
            }

            this.Dialog = null;
        }

        private void OnCancelConnecting()
        {
            this.State = MainViewModelState.Cancelling;

            this.ShowDialogMessage(Resources.Strings.FAILED_TO_ESTABLISH_CONNECTION_CANCELLED, onClose: () =>
            {
                this.Dialog = null;
                this.State = MainViewModelState.Ready;
            });
        }

        private void OnErrorWhileConnectingOrGetData(Exception exception)
        {
            this.logger.LogTrace("connectingViewModel onError");

            this.logger.LogError(exception, Resources.Strings.ERROR_DATA_READING);

            this.State = MainViewModelState.Ready;

            this.Dialog = null;

            if (exception is System.TimeoutException te)
            {
                this.ShowDialogMessage($"{Resources.Strings.FAILED_TO_ESTABLISH_CONNECTION_TIMEOUT}");
            }
            else
            {
                string message = Resources.Strings.FAILED_TO_ESTABLISH_CONNECTION_TIMEOUT;
                string detailedMessage = string.Empty;

                if (exception is System.Net.Sockets.SocketException se)
                {
                    switch (se.ErrorCode)
                    {
                        case 10050:
                            detailedMessage = Common.Resources.NetErrors.WSAENETDOWN;
                            break;

                        case 10053:
                            detailedMessage = Common.Resources.NetErrors.WSAECONNABORTED;
                            break;

                        case 10060:
                            detailedMessage = Common.Resources.NetErrors.WSAETIMEDOUT;
                            break;

                        case 10061:
                            detailedMessage = Common.Resources.NetErrors.WSAECONNREFUSED;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    detailedMessage = $"{Resources.Strings.ERROR_OCCURED}:\n{exception.Message}";
                }

                this.ShowDialogMessage(message, detailedMessage);
            }
        }

        private async Task<bool> GetDeviceInfoAsync(Device device, CancellationToken cancellationToken)
        {
            device.NumberOfConnectionAttemps++;

            string[] dots = ["ꞏ", "꞉", "⁞", "꞉"];
            int dotIndex = 0;

            void updateStatusTimerCallback(object? state)
            {
                device.Status = dots[dotIndex++];

                if (dotIndex >= 4)
                {
                    dotIndex = 0;
                }
            }

            var timer = new System.Threading.Timer(callback: updateStatusTimerCallback, null, 0, 200);

            async Task onConnectToDevice(CommunicatorTcpClient client)
            {
                string message = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Strings.SUCCESSFULLY_CONNECTED_TO, device.IpAddress, CommunicatorTcpClient.SETUP_PORT);
                this.logger.LogDebug(message);

                // чтение информации об устройстве
                var deviceInfo = await client.ReadInfoAsync();

                device.DeviceName = deviceInfo.Name;
                device.DeviceFirmwareVersion = deviceInfo.FirmwareVersion;
                device.DeviceFirmwareDate = deviceInfo.FirmwareDate;

                // Проверка уровня сигнала и типа связи
                var (networkMode, signalStrength) = await client.ReadNetworkModeAndLevelAsync();

                device.DeviceNetworkType = networkMode;
                device.NetLevel = signalStrength;

                timer.Dispose();

                device.Status = "✔️";
            }

            void onErrorGetData(Exception e)
            {
                this.logger.LogError($"Get info for {device} failed.");
                timer.Dispose();
                device.Status = "❌";
            }

            var tcpClient = new CommunicatorTcpClient(this.logger, this.Settings, cancellationToken)
            {
                OnConnect = onConnectToDevice,

                OnError = onErrorGetData
            };

            // пытаемся подключиться
            bool isOk = await tcpClient.ConnectAsync(device.IpAddress);

            tcpClient.Close();

            return isOk;
        }

        #region Commands

        private bool CanConnect()
        {
            bool isCancellationRequested = this.cancellationTokenSource is not null && this.cancellationTokenSource.IsCancellationRequested;

            var (ipAddressIsValid, validationMessage) = Common.Utils.IPAddressValidator.Validate(this.Settings.IpAddress);

            return isCancellationRequested == false && string.IsNullOrWhiteSpace(this.Settings.IpAddress) == false && ipAddressIsValid;
        }

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task Connect()
        {
            if (this.isConnected)
            {
                this.logger.LogTrace("Disconnect");

                this.IsConnected = false;

                this.communicatorTcpClient?.Close();

                this.State = MainViewModelState.Ready;

                return;
            }

            this.logger.LogTrace("ConnectAsync");

            this.State = MainViewModelState.Connecting;

            if (this.cancellationTokenSource == null || this.cancellationTokenSource.TryReset() == false)
            {
                this.cancellationTokenSource = new CancellationTokenSource();
            }

            this.connectingViewModel = new ConnectingViewModel(this.logger, this.cancellationTokenSource, this.Settings.ConnectionWaitTimeout);

            this.connectingViewModel.StartTimers();

            this.communicatorTcpClient = new CommunicatorTcpClient(this.logger, this.Settings, this.cancellationTokenSource.Token, this.connectingViewModel)
            {
                OnConnect = this.OnConnect,

                OnCancel = this.OnCancelConnecting,

                OnError = this.OnErrorWhileConnectingOrGetData
            };

            this.ShowDialog(this.connectingViewModel);

            await this.communicatorTcpClient.ConnectAsync();
        }

        private bool CanSaveDeviceConfig()
        {
            return this.isConnected && this.communicatorTcpClient != null && (this.deviceConfig != this.newDeviceConfig | this.deviceRs485Config != this.newRs485Config);
        }

        [RelayCommand(CanExecute = nameof(CanSaveDeviceConfig))]
        private void SaveDeviceConfig()
        {
            if (this.communicatorTcpClient == null)
            {
                return;
            }

            this.ShowDialogMessage(Resources.Strings.NEW_DEVICE_SETTINGS_IS_SAVING);

            if (this.newDeviceConfig == null || this.newRs485Config == null)
            {
                this.logger.LogWarning(Resources.Strings.NEW_DEVICE_SETTINGS_IS_NOT_DEFINED);
                this.ShowDialogMessage(Resources.Strings.NEW_DEVICE_SETTINGS_IS_NOT_DEFINED);
            }
            else
            {
                bool result = false;

                Task.Run(async () =>
                {
                    bool result = await this.communicatorTcpClient.WriteDeviceConfig(this.newDeviceConfig, this.newRs485Config);
                })
                    .ContinueWith(async t =>
                    {
                        this.Dialog = null;

                        await Task.Delay(500);

                        if (result)
                        {
                            this.logger.LogTrace(Resources.Strings.NEW_DEVICE_SETTINGS_SAVED);
                            this.ShowDialogMessage(Resources.Strings.NEW_DEVICE_SETTINGS_SAVED);
                        }
                        else
                        {
                            this.logger.LogTrace(Resources.Strings.ERROR_OCCURED_NEW_DEVICE_SETTINGS_NOT_SAVED);
                            this.ShowDialogMessage(Resources.Strings.ERROR_OCCURED_NEW_DEVICE_SETTINGS_NOT_SAVED);
                        }
                    });
            }
        }

        private bool CanStartCheckDevices()
        {
            bool isCancellationRequested = this.cancellationTokenSource is not null && this.cancellationTokenSource.IsCancellationRequested;

            return isCancellationRequested == false && this.Devices.Count > 0;
        }

        [RelayCommand(CanExecute = nameof(CanStartCheckDevices), IncludeCancelCommand = true)]
        private async Task StartCheckDevicesAsync(CancellationToken token)
        {
            this.IsDevicesChecked = false;

            try
            {
                this.logger.LogTrace(Resources.Strings.START_DEVICES_CHECK);

                this.State = MainViewModelState.CheckingDevices;

                foreach (var device in this.Devices)
                {
                    device.Status = string.Empty;
                }

                System.Collections.Concurrent.ConcurrentQueue<Device> queue = new System.Collections.Concurrent.ConcurrentQueue<Device>(this.Devices);
                int total = this.Devices.Count;
                int completed = 0;
                while (!queue.IsEmpty)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    Device? device = new();

                    if (queue.TryDequeue(out device))
                    {
                        this.CheckingDevicesState = string.Format(
                            this.Settings.SelectedCultureInfo,
                            Common.Resources.UI_strings.Checking_devices_progress,
                            completed,
                            total,
                            100.0d * completed / total
                        );

                        this.CheckingDevicesState += System.Environment.NewLine;

                        this.CheckingDevicesState += string.Format(
                            this.Settings.SelectedCultureInfo,
                            Common.Resources.UI_strings.Checking_devices_state,
                            device.Name,
                            device.NumberOfConnectionAttemps + 1
                        );

                        bool isOk = await this.GetDeviceInfoAsync(device, token);

                        if (isOk == false)
                        {
                            if (device.NumberOfConnectionAttemps <= this.Settings.RetryCount)
                            {
                                queue.Enqueue(device);
                            }
                        }
                    }
                }

                this.IsDevicesChecked = true;
            }
            catch (Exception e)
            {
                this.IsDevicesChecked = false;

                this.logger.LogError(e, Resources.Strings.ERROR_OCCURED);
            }
            finally
            {
                this.IsDevicesChecking = false;

                this.State = MainViewModelState.Ready;

                this.logger.LogTrace("MainViewModel: end devices checking");
            }
        }

        private bool CanPasteListFromClipboard()
        {
            return this.Devices.Count == 0 && this.IsDevicesChecking == false;
        }

        [RelayCommand(CanExecute = nameof(CanPasteListFromClipboard))]
        private void PasteListFromClipboard()
        {
            if (System.Windows.Clipboard.ContainsText(System.Windows.TextDataFormat.CommaSeparatedValue))
            {
                IList<IList<string>>? list;

                string csvData = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.CommaSeparatedValue);

                if (string.IsNullOrEmpty(csvData))
                {
                    csvData = System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.UnicodeText);

                    list = Utils.TableHelper.ParseTable(csvData, '\t');
                }
                else
                {
                    list = Utils.TableHelper.ParseTable(csvData, ';');
                }

                if (list != null && list.Count > 0)
                {
                    this.Devices.Clear();

                    foreach (var item in list)
                    {
                        var (ipAddressIsValid, validationMessage) = Common.Utils.IPAddressValidator.Validate(item[1]);

                        if (ipAddressIsValid)
                        {
                            Model.Device device = new()
                            {
                                Name = item[0],
                                IpAddress = item[1]
                            };

                            this.Devices.Add(device);
                        }
                    }

                    int skipped = list.Count - this.Devices.Count;
                    string skippedMessage = skipped > 0 ? string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Strings.SKIPPED_N_LINES, skipped) : ".";

                    this.ShowDialogMessage(string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Strings.LOADED_N_LINES, this.Devices.Count, skippedMessage));
                }
            }

            this.ClearListCommand.NotifyCanExecuteChanged();
            this.StartCheckDevicesCommand.NotifyCanExecuteChanged();
            this.PasteListFromClipboardCommand.NotifyCanExecuteChanged();
        }

        private bool CanCopyResultToClipboard()
        {
            return this.Devices.Count > 0 && this.isDevicesChecked;
        }

        [RelayCommand(CanExecute = nameof(CanCopyResultToClipboard))]
        private void CopyResultToClipboard()
        {
#pragma warning disable CA1859 // Use concrete types when possible for improved performance
            IList<IList<string>> stringData = new List<IList<string>>(this.Devices.Count);
#pragma warning restore CA1859 // Use concrete types when possible for improved performance

            // 8 columns: Name, IpAddress, DeviceName, NetLevel, NetMode, DeviceFirmwareVersion, DeviceFirmwareDate, NumberOfConnectionAttemps
            List<string> fields = new List<string>(8);
            foreach (var device in this.Devices)
            {
                fields =
                [
                    device.Name,
                    device.IpAddress,
                    device.DeviceName,
                    device.NetLevel.ToString(CultureInfo.InvariantCulture),
                    device.NetMode,
                    device.DeviceFirmwareVersion,
                    device.DeviceFirmwareDate,
                    device.NumberOfConnectionAttemps.ToString(CultureInfo.InvariantCulture),
                ];

                stringData.Add(fields);
            }

            string csvData = Utils.TableHelper.ToCsvString(stringData);

            try
            {
                System.Windows.DataObject dataObject = new System.Windows.DataObject();

                // Add tab-delimited text to the container object as is.
                dataObject.SetText(csvData.Replace(';', '\t'));

                // Convert the CSV text to a UTF-8 byte stream before adding it to the container object.
                var bytes = System.Text.Encoding.UTF8.GetBytes(csvData);
                var stream = new System.IO.MemoryStream(bytes);
                dataObject.SetData(System.Windows.DataFormats.CommaSeparatedValue, stream);
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "Clipboard error:");
            }
        }

        private bool CanPasteListFromFile()
        {
            return this.isExcelConfigured;
        }

        [RelayCommand(CanExecute = nameof(CanPasteListFromFile))]
        private void PasteListFromFile()
        {
            this.ClearListCommand.NotifyCanExecuteChanged();
            this.StartCheckDevicesCommand.NotifyCanExecuteChanged();
        }

        private bool CanWriteResultToFile()
        {
            return this.isExcelConfigured && this.Devices.Count > 0 && this.isDevicesChecked;
        }

        [RelayCommand(CanExecute = nameof(CanWriteResultToFile))]
        private void WriteResultToFile()
        {
            throw new NotImplementedException();
        }

        private bool CanSetupExcel()
        {
            return true;
        }

        [RelayCommand(CanExecute = nameof(CanSetupExcel))]
        private void SetupExcel()
        {
            this.isExcelConfigured = true;
        }

        private bool CanClearList()
        {
            return this.Devices.Count > 0 && this.isDevicesChecking == false;
        }

        [RelayCommand(CanExecute = nameof(CanClearList))]
        private void ClearList()
        {
            this.Devices.Clear();

            this.ClearListCommand.NotifyCanExecuteChanged();
            this.StartCheckDevicesCommand.NotifyCanExecuteChanged();

            this.CopyResultToClipboardCommand.NotifyCanExecuteChanged();
            this.WriteResultToFileCommand.NotifyCanExecuteChanged();

            this.PasteListFromClipboardCommand.NotifyCanExecuteChanged();
        }

        #endregion

        #endregion

        #region Public properties

        public AppSettings Settings { get; private set; }

        /// <summary>
        /// Флаг, указывающий на доступность пользовательского интерфейса
        /// </summary>
        public bool IsReady => !this.IsBusy;

        /// <summary>
        /// Заголовок кнопки подключиться/отключиться
        /// </summary>
#pragma warning disable IDE0072 // Add missing cases
        public string ConnectCommandHeader => this.state switch
        {
            MainViewModelState.Ready => Resources.Strings.CONNECT_COMMAND_HEADER,
            MainViewModelState.Connecting or MainViewModelState.Connected => Resources.Strings.DISCONNECT_COMMAND_HEADER,
            MainViewModelState.Cancelling => Resources.UI_strings.IS_CANCELLING_COMMAND_HEADER,
            _ => Resources.Strings.CONNECT_COMMAND_HEADER,
        };

        /// <summary>
        /// Сообщение для пользователя
        /// </summary>
        public string Message => this.state switch
        {
            MainViewModelState.Ready => Resources.Strings.DEFAULT_READY_MESSAGE,
            MainViewModelState.Connecting => string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Strings.TRY_TO_CONNECT_IP_AND_PORT, this.Settings.IpAddress, CommunicatorTcpClient.SETUP_PORT),
            MainViewModelState.Connected => Resources.Strings.CONNECTED_DATA_READED,
            MainViewModelState.CheckingDevice => Resources.Strings.CHECK_NET_LEVEL_MESSAGE,
            MainViewModelState.CheckingDevices => Resources.Strings.DEVICES_CHECKING,
            _ => Resources.Strings.DEFAULT_READY_MESSAGE,
        };
#pragma warning restore IDE0072 // Add missing cases

        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            this.logger.LogTrace("Dispose");

            this.ShowDialogMessage("Завершение работы приложения ...");

            this.Settings.PropertyChanged -= this.Settings_PropertyChanged;

            this.connectingViewModel?.Dispose();

            this.cancellationTokenSource?.Dispose();

            this.communicatorTcpClient?.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
