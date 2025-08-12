namespace TMP.Work.CommunicatorPSDTU.Common;

using System;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using PropertyChanged.SourceGenerator;

using Model;
using ViewModels;

public partial class CommunicatorTcpClient : IDisposable
{
#pragma warning disable CA1707 // Identifiers should not contain underscores
    public const int SETUP_PORT = 1001;
#pragma warning restore CA1707 // Identifiers should not contain underscores

    /// <summary>
    /// Флаг наличия подключения
    /// </summary>
    [Notify(set: Setter.Private)] private bool isConnected;

    private readonly ILogger logger;

    private readonly TcpClient tcpClient;

    private readonly byte[] buffer = new byte[100];

    private readonly AppSettings appSettings;

    private readonly CancellationToken cancellationToken;

    private readonly Parser.FunctionsAnswerParser answerParser;

    private readonly ConnectingViewModel? connectingViewModel;

    public CommunicatorTcpClient(ILogger logger, AppSettings appSettings, CancellationToken cancellationToken, ConnectingViewModel? connectingViewModel = null)
    {
        this.logger = logger;
        this.logger.LogTrace("CommunicatorTcpClient ctor");

        this.answerParser = new(logger);

        this.appSettings = appSettings;
        this.cancellationToken = cancellationToken;
        this.connectingViewModel = connectingViewModel;

        this.tcpClient = new();
    }

    #region Private methods

    private void ConfigureTcpClient()
    {
        this.tcpClient?.ReceiveTimeout = this.appSettings.SendOrReceiveTimeout * 1_000;
        this.tcpClient?.SendTimeout = this.appSettings.SendOrReceiveTimeout * 500;
    }

    private async Task<(int sendedBytesCount, Exception? lastException)> TrySendDataAsync(ArraySegment<byte> request, Action<string> uiCallback)
    {
        int sendedBytesCount = 0;
        int currentAttempt = 0;
        Exception? lastException = null;

        if (this.tcpClient == null)
        {
            this.logger.LogError("TrySendData: tcpSocket == null");
            return (-1, new System.ObjectDisposedException(null));
        }

        if (this.tcpClient.Connected == false)
        {
            this.logger.LogError("TrySendData: tcpSocket is not connected");
            return (-1, new System.InvalidOperationException(null));
        }

        var sendCts = new CancellationTokenSource();

        sendCts.CancelAfter(this.appSettings.SendOrReceiveTimeout * 500);

        while (currentAttempt < this.appSettings.RetryCount)
        {
            currentAttempt++;

            this.logger.LogTrace("Send: Attempt #{CurrentAttempt}", currentAttempt);

            uiCallback($"{Common.Resources.UI_strings.SendingData} {Common.Resources.UI_strings.Attempt} {currentAttempt}");

            this.connectingViewModel?.ResetTimeout((ushort)(this.appSettings.SendOrReceiveTimeout / 2));

            if (this.cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                sendedBytesCount = await this.tcpClient.Client.SendAsync(request, sendCts.Token);

                break;
            }
            catch (System.Net.Sockets.SocketException se)
            {
                this.logger.LogWarning(se, "TrySendData: Network error :: ");
                lastException = se;
            }
            catch (TaskCanceledException tce)
            {
                this.logger.LogWarning(tce, "TrySendData: Timeout waiting for data");
                lastException = tce;

                uiCallback($"{Common.Resources.UI_strings.SendingData} {Common.Resources.UI_strings.Pause}");

                await Task.Delay(this.appSettings.DelayBetweenRetries).ConfigureAwait(false);

                if (!sendCts.TryReset())
                {
                    sendCts = new CancellationTokenSource();
                    sendCts.CancelAfter(this.appSettings.SendOrReceiveTimeout * 500);
                }
            }
            catch (OperationCanceledException oce)
            {
                this.logger.LogWarning(oce, "TrySendData: Timeout sending data");
                lastException = oce;

                uiCallback($"{Common.Resources.UI_strings.SendingData} {Common.Resources.UI_strings.Pause}");

                await Task.Delay(this.appSettings.DelayBetweenRetries).ConfigureAwait(false);

                if (!sendCts.TryReset())
                {
                    sendCts = new CancellationTokenSource();
                    sendCts.CancelAfter(this.appSettings.SendOrReceiveTimeout * 500);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "TrySendData: Error sending data");
                lastException = ex;

                break;
            }
        }

        return (sendedBytesCount, lastException);
    }

    private async Task<int> SendRequestAndReceiveDataAsync(string operationDescriptionForLogging, ArraySegment<byte> request)
    {
        if (this.tcpClient == null)
        {
            this.logger.LogError("SendRequestAndReceiveData: tcpSocket == null");
            return -1;
        }

        if (this.tcpClient.Connected == false)
        {
            this.logger.LogError("SendRequestAndReceiveData: tcpSocket is not connected");
            return -1;
        }

        void uiCallback(string message) => this.connectingViewModel?.DetailMessage = operationDescriptionForLogging + Environment.NewLine + message;

        try
        {
            Array.Clear(this.buffer, 0, this.buffer.Length);

            this.logger.LogTrace("ReceiveData: Attempt to send > {Count}", Convert.ToHexString(request));

            (int sendedBytesCount, Exception? lastException) = await this.TrySendDataAsync(request, uiCallback);

            if (sendedBytesCount == -1)
            {
                this.logger.LogCritical(lastException, "SendData: Nothing sent - an error occurred.");
                return -1;
            }

            if (sendedBytesCount == 0)
            {
                this.logger.LogCritical(lastException, "SendData: Nothing sent - operation aborted.");
                return -1;
            }

            if (sendedBytesCount != request.Count)
            {
                this.logger.LogWarning("SendData: Sent {SendedBytesCount} bytes, expected to send {Length} bytes.", sendedBytesCount, request.Count);
            }
            else
            {
                this.logger.LogTrace("SendData: Sent {SendedBytesCount} bytes.", sendedBytesCount);
            }

            var receiveCts = new CancellationTokenSource();

            receiveCts.CancelAfter(this.appSettings.SendOrReceiveTimeout * 1_000);

            int received = 0;
            int currentAttempt = 0;

            while (currentAttempt < this.appSettings.RetryCount)
            {
                currentAttempt++;

                this.logger.LogTrace("ReceiveData: Attempt #{CurrentAttempt}", currentAttempt);

                uiCallback($"{Common.Resources.UI_strings.GettingData} {Common.Resources.UI_strings.Attempt} {currentAttempt}");

                this.connectingViewModel?.ResetTimeout(this.appSettings.SendOrReceiveTimeout);

                if (this.cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    received = await this.tcpClient.Client.ReceiveAsync(this.buffer, receiveCts.Token);

                    this.logger.LogTrace($"Available {this.tcpClient.Client.Available} bytes.");

                    break;
                }
                catch (System.Net.Sockets.SocketException se)
                {
                    this.logger.LogWarning(se, "ReceiveData: Network error :: ");
                    lastException = se;
                }
                catch (TaskCanceledException tce)
                {
                    this.logger.LogWarning(tce, "ReceiveData: Timeout waiting for data");
                    lastException = tce;

                    uiCallback($"{Common.Resources.UI_strings.GettingData} {Common.Resources.UI_strings.Pause}");

                    await Task.Delay(this.appSettings.DelayBetweenRetries).ConfigureAwait(false);

                    if (!receiveCts.TryReset())
                    {
                        receiveCts = new CancellationTokenSource();
                        receiveCts.CancelAfter(this.appSettings.SendOrReceiveTimeout * 1_000);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    this.logger.LogWarning(oce, "ReceiveData: Timeout waiting for data");
                    lastException = oce;

                    uiCallback($"{Common.Resources.UI_strings.GettingData} {Common.Resources.UI_strings.Pause}");

                    await Task.Delay(this.appSettings.DelayBetweenRetries).ConfigureAwait(false);

                    if (!receiveCts.TryReset())
                    {
                        receiveCts = new CancellationTokenSource();
                        receiveCts.CancelAfter(this.appSettings.SendOrReceiveTimeout * 1_000);
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "ReceiveData: Error while receiving data");
                    lastException = ex;

                    break;
                }
            }

            this.logger.LogTrace("Received < {Received} bytes : {BytesArrayAsHex}, number of attempts: {Attempts}", received, Convert.ToHexString(this.buffer, 0, received), currentAttempt);

            return received;
        }
        catch (Exception e)
        {
            this.logger.LogCritical(e, "SendRequestAndReceiveDataAsync : An error occurred.");
            return -1;
        }
    }

    private bool CheckReceivedAndExpectedBytesCount(int received, int expectedReceiveBytesCount)
    {
        if (received > 0 && received != expectedReceiveBytesCount)
        {
            this.logger.LogWarning("Received {Received} bytes, expected to receive {Expected} bytes.", received, expectedReceiveBytesCount);

            return false;
        }
        else
        {
            return true;
        }
    }

    #endregion

    #region Public methods

    public void Close()
    {
        this.tcpClient?.Close();
        this.tcpClient?.Dispose();
    }

    public async Task<bool> ConnectAsync(string? ipaddress = null)
    {
        this.logger.LogTrace("CommunicatorTcpClient ConnectAsync");

        this.connectingViewModel?.DetailMessage = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Strings.TRY_TO_CONNECT_IP_AND_PORT, this.appSettings.IpAddress, SETUP_PORT);

        try
        {
            var waitConnectionTask = Task.Delay(this.appSettings.ConnectionWaitTimeout * 1_000);

            if (string.IsNullOrEmpty(ipaddress))
            {
                ipaddress = this.appSettings.IpAddress;
            }

            // пытаемся подключиться используя URL-адрес и порт
            var connectTask = this.tcpClient.ConnectAsync(ipaddress, SETUP_PORT, this.cancellationToken);

            //double await so if cancelTask throws exception, this throws it
            var task = await Task.WhenAny(connectTask.AsTask(), waitConnectionTask);

            await task;

            if (connectTask.IsFaulted)
            {
                this.logger.LogTrace("ConnectingViewModel: connectTask is faulted.");
            }

            if (waitConnectionTask.IsFaulted)
            {
                this.logger.LogTrace("ConnectingViewModel: waitConnectionTask is faulted.");
            }

            if (waitConnectionTask.IsCompleted)
            {
                this.logger.LogTrace("ConnectingViewModel: waitConnectionTask is completed.");

                this.OnError?.Invoke(new TimeoutException());
                return false;
            }

            if (connectTask.IsCompleted)
            {
                this.logger.LogTrace("ConnectingViewModel: connectTask is completed.");

                this.IsConnected = true;

                if (this.OnConnect != null)
                {
                    await this.OnConnect.Invoke(this);
                }

                return true;
            }
        }
        catch (SocketException se)
        {
            if (se.SocketErrorCode == SocketError.TimedOut)
            {
                this.logger.LogWarning(se, Resources.Strings.CONNECT_ERROR);
            }
            else
            {
                this.logger.LogCritical(se, Resources.Strings.CONNECT_ERROR);
            }

            this.OnError?.Invoke(se);
        }
        catch (OperationCanceledException oce)
        {
            this.logger.LogWarning(oce, Resources.Strings.OPERATION_CANCELED);

            this.OnError?.Invoke(oce);
        }
        catch (Exception e)
        {
            var trace = TMP.Shared.Common.Utils.GetExceptionDetails(e);

            string message = Resources.Strings.ERROR_OCCURED + " :: {Msg}\n" + trace;

            this.logger.LogCritical(message);

            this.OnError?.Invoke(e);
        }

        return false;
    }

    /// <summary>
    /// чтение информации об устройстве
    /// </summary>
    /// <returns>информация об устройстве</returns>
    public async Task<Info> ReadInfoAsync()
    {
        this.logger.LogTrace("ReadInfo: start reading version");

        Model.Info result = new();

        int receivedBytes = await this.SendRequestAndReceiveDataAsync(Resources.UI_strings.ReadingVersion, Requests.ReadVersionRequest);

        if (receivedBytes > 0)
        {
            this.answerParser.ParseInfo(this.buffer, ref result);

            this.logger.LogTrace("ReadInfo result: {Result}", result);
        }
        else
        {
            this.logger.LogWarning("ReadInfo: The operation failed.");
        }

        return result;
    }

    /// <summary>
    /// чтение конфигурации сети
    /// </summary>
    /// <returns>конфигурация сети</returns>
    public async Task<Model.NetworkConfig> ReadNetworkConfigAsync()
    {
        this.logger.LogTrace("ReadNetworkConfig: start reading network configuration");

        Model.NetworkConfig networkConfig = new();

        int receivedBytes = await this.SendRequestAndReceiveDataAsync(Resources.UI_strings.ReadingNetworkConfiguration, Requests.ReadNetworkConfigRequest);

        this.answerParser.ParseNetworkConfig(this.buffer.AsSpan(0, receivedBytes), ref networkConfig);
        this.logger.LogTrace("ReadNetworkConfig result: {Result}", networkConfig);

        return networkConfig;
    }

    /// <summary>
    /// чтение уровня сигнала и режима сети
    /// </summary>
    /// <returns>режим сети и уровень сигнала</returns>
    public async Task<(string NetMode, byte NetLevel)> ReadNetworkModeAndLevelAsync()
    {
        this.logger.LogTrace("ReadNetworkModeAndLevel: start reading signal strength and network mode");

        (string netMode, byte netLevel) result = (string.Empty, 0);

        int receivedBytes = await this.SendRequestAndReceiveDataAsync(Resources.UI_strings.CheckingSignalLevelAndConnectionType, Requests.ReadNetworkLevelRequest);

        if (receivedBytes > 0)
        {
            this.answerParser.ParseNetworkModeAndLevel(this.buffer.AsSpan(0, receivedBytes), ref result);
        }

        this.logger.LogTrace("ReadNetworkModeAndLevel result: {NetMode}, signal strength: {NetLevel}.", result.netMode, result.netLevel);

        return result;
    }

    /// <summary>
    /// Чтение конфигурации
    /// </summary>
    /// <returns>конфигурация</returns>
    public async Task<(Model.Config DeviceConfig, Model.SerialConfig SerialConfig)> ReadConfig()
    {
        this.logger.LogTrace("ReadConfig: start reading configuration");

        (Model.Config deviceConfig, Model.SerialConfig serialConfig) result = new(new Config(), new SerialConfig());

        int receivedBytes = await this.SendRequestAndReceiveDataAsync(Resources.UI_strings.ReadingDeviceConfiguration, Requests.ReadConfigRequest);

        if (receivedBytes > 0)
        {
            this.answerParser.ParseConfig(this.buffer.AsSpan(0, receivedBytes), ref result);

            this.logger.LogTrace("ReadConfig configuration: {Config}.", result);
        }
        else
        {
            this.logger.LogWarning("ReadConfig: the operation failed.");
        }

        return result;
    }

    public async Task<bool> WriteDeviceConfig(Model.Config deviceConfig, Model.SerialConfig serialConfig)
    {
        this.logger.LogTrace("WriteDeviceConfig: start recording new configuration");

        ArraySegment<byte> packet = Builder.RequestBuilder.WriteDeviceConfigRequest(deviceConfig, serialConfig, this.logger);

        int receivedBytes = await this.SendRequestAndReceiveDataAsync(Resources.UI_strings.IsWritingNewDeviceConfiguration, packet);

        if (receivedBytes >= 6)
        {
            if (Utils.PacketOfBytes.CheckModbusFunctionAndCrc(this.buffer.AsSpan(0, receivedBytes), 0x10, this.logger) == ModbusRtuPacketStatus.Success)
            {
                // При успешной отправке запроса будет возвращен ответ вида 01 10 FC 00 00 29 31 87

                byte errorCode = this.buffer[3];

                switch (errorCode)
                {
                    case 0x0:
                        this.logger.LogInformation("WriteDeviceConfig: there are no errors.");
                        return true;
                    case 0x02:
                        this.logger.LogInformation("WriteDeviceConfig: : invalid write address. Error in Write Address or Number of Write Registers fields..");
                        break;
                    case 0x03:
                        this.logger.LogInformation("WriteDeviceConfig: error in data fields.");
                        break;
                    case 0x06:
                        this.logger.LogInformation("WriteDeviceConfig: error in CRC16 field, the checksum of the parcel is not calculated correctly.");
                        break;
                    default:
                        this.logger.LogInformation("WriteDeviceConfig: unknown error code: {ErrorCode}", errorCode);
                        break;
                }
            }

            return false;
        }
        else
        {
            this.logger.LogWarning("WriteDeviceConfig: the operation failed.");

            return false;
        }
    }

    #endregion

    #region Public properties

    public bool Connected => this.tcpClient is not null && this.tcpClient.Connected != false;

    public Func<CommunicatorTcpClient, Task>? OnConnect { get; set; }

    public Action<Exception>? OnError { get; set; }

    public Action? OnCancel { get; set; }

    #endregion

    #region IDisposable implementation
    public void Dispose()
    {
        this.logger.LogTrace("Dispose");

        this.connectingViewModel?.Dispose();

        this.tcpClient?.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}
