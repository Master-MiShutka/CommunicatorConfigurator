namespace TMP.Work.CommunicatorPSDTU.Common.Parser;

using System;
using System.Text;
using Microsoft.Extensions.Logging;
using TMP.Work.CommunicatorPSDTU.Common.Model;
using TMP.Work.CommunicatorPSDTU.Common.Utils;

public sealed class FunctionsAnswerParser
{
    private readonly ILogger logger;

    public FunctionsAnswerParser(ILogger logger)
    {
        this.logger = logger;
        this.logger.LogTrace("FunctionsAnswerParser ctor");
    }

    public void ParseInfo(ReadOnlySpan<byte> bytes, ref Model.Info result)
    {
        // строка вида Communicator GPRS-3G-LTE FirmWare:07.130523

        ModbusRtuPacket packet = new ModbusRtuPacket(bytes);

        if (packet.Crc16 != packet.CalculatedCrc16)
        {
            this.logger.LogError("The checksum of the data packet does not match! In the packet {CrcPacket}, and the disheveled one {CrcCalculated:X2}.", packet.Crc16, packet.CalculatedCrc16);

            // return;
        }

        string data = Encoding.ASCII.GetString(packet.Body);

        int pos = data.IndexOf("FirmWare", StringComparison.InvariantCulture);

        if (pos == -1)
        {
            result.Name = data;
            this.logger.LogWarning("Could not find substring 'FirmWare'.");
        }
        else
        {
            string s = data[..(pos - 2)];

            result.Name = s.StartsWith('+') ? data.Substring(1, pos - 2) : data[..(pos - 2)];

            s = data[(pos + "FirmWare".Length + 1)..];

            pos = s.IndexOf('.');

            if (pos == -1)
            {
                result.Name = data;
                this.logger.LogWarning("Failed to find substring '.'");
            }
            else
            {
                result.FirmwareVersion = s[..pos];

                if (pos + 7 > s.Length)
                {
                    this.logger.LogWarning("The line is too short to parse..");
                }
                else
                {
                    result.FirmwareDate = s.Substring(pos + 1, 6);
                }
            }
        }
    }

    public void ParseNetworkConfig(ReadOnlySpan<byte> bytes, ref Model.NetworkConfig networkConfig)
    {
        // 01 03 04 | 00 0C 1A 00 | 31 50 - HEX
        // 01 03 04 | 00 12 26 00 | 49 80 - DEC

        // 01 83 01   03 B1 A1

        if (Utils.PacketOfBytes.CheckModbusFunctionAndCrc(bytes, 0x03, this.logger) != ModbusRtuPacketStatus.Success)
        {
            byte networkType = bytes[3];

            networkConfig.NetworkType = networkType switch
            {
                0 => "Auto",
                1 => "2G (GSM)",
                2 => "3G",
                3 => "4G (LTE)",
                _ => Resources.Strings.UnknownData,
            };

            this.logger.LogTrace(":: networkType={NetworkType} [{NetworkTypeStr}]", networkType, networkConfig.NetworkType);

            byte networkPriority = bytes[4];
            string networkPriorityStr = string.Empty;

            networkConfig.NetworkPriority = networkPriority switch
            {
                0 => "Auto",
                1 => "2G only",
                2 => "3G only",
                3 => "4G only",
                4 => "2G/3G/4G",
                5 => "3G/2G/4G",
                6 => "4G/3G",
                7 => "4G/2G",
                8 => "3G/4G",
                9 => "3G/2G",
                10 => "2G/4G",
                11 => "2G/3G",
                12 => "4G/3G/2G",
                _ => Resources.Strings.UnknownData,
            };

            this.logger.LogTrace(":: networkPriority={NetworkPriority} [{NetworkPriorityStr}]", networkPriority, networkConfig.NetworkPriority);

            byte networkFrequency = bytes[5];
            string networkFrequencyStr = string.Empty;

            if ((networkFrequency & 0x20) != 0)
            {
                networkConfig.NetworkFrequency += "2600 MHz, 4G; ";
            }
            if ((networkFrequency & 0x10) != 0)
            {
                networkConfig.NetworkFrequency += "1800 MHz, 4G; ";
            }
            if ((networkFrequency & 0x08) != 0)
            {
                networkConfig.NetworkFrequency += "2100 MHz, 3G; ";
            }
            if ((networkFrequency & 0x04) != 0)
            {
                networkConfig.NetworkFrequency += "900 MHz, 3G; ";
            }
            if ((networkFrequency & 0x02) != 0)
            {
                networkConfig.NetworkFrequency += "1800 MHz, 2G; ";
            }
            if ((networkFrequency & 0x01) != 0)
            {
                networkConfig.NetworkFrequency += "900 MHz, 2G; ";
            }

            networkConfig.NetworkFrequency = networkConfig.NetworkFrequency.TrimEnd([';', ' ']);

            this.logger.LogTrace(":: networkFrequency={NetworkFrequency} [{NetworkFrequencyStr}]", networkFrequency, networkConfig.NetworkFrequency);
        }
    }

    public void ParseNetworkModeAndLevel(ReadOnlySpan<byte> bytes, ref (string NetMode, byte NetLevel) result)
    {
        // 01 03 02 80 0F 99 80
        // 01 03 02 08 10 BE 48

        if (Utils.PacketOfBytes.CheckModbusFunctionAndCrc(bytes, 0x03, this.logger) == ModbusRtuPacketStatus.Success)
        {
            byte frequencyType = bytes[3];

            result.NetMode = frequencyType switch
            {
                1 or 2 => "2G",
                16 or 128 => "3G",
                4 or 8 => "4G",
                _ => Resources.Strings.UnknownData,
            };

            this.logger.LogTrace(":: frequencyType={FrequencyType} [{FrequencyTypeStr}]", frequencyType, result.NetMode);

            byte networkLevel = bytes[4];

            this.logger.LogTrace(":: networkLevel={NetworkLevel}", networkLevel);

            if (networkLevel is >= 100 and <= 131)
            {
                networkLevel -= 100;
            }

            if (networkLevel is >= 150 and <= 181)
            {
                networkLevel -= 150;
            }

            if (networkLevel is >= 200 and <= 231)
            {
                networkLevel -= 200;
            }

            result.NetLevel = networkLevel;
        }
    }

    public void ParseConfig(ReadOnlySpan<byte> bytes, ref (Model.Config DeviceConfig, Model.SerialConfig SerialConfig) result)
    {
        // 0103260B76706E322E6D74732E62790376706E0A3226316C3127303E3F60043430303101360300000026B1 - 43 bytes
        // 01 03 26 | 0B 76706E322E6D74732E6279 | 03 76706E | 0A 3226316C3127303E3F60 | 04 34303031 | 01 36 | 0300000026B1
        //
        // 01 - addr
        // 03 - function
        // 26 - next bytes count (hex) - 40 bytes
        //
        // 0B - length of apn - 11 chars
        // 76706E322E6D74732E6279 - apn = "vpn2.mts.by"
        //
        // 03 - length of login - 3 chars
        // 76706E - login = "vpn"
        //
        // 0A - length of password - 10 chars
        // 3226316C3127303E3F60 - password = ""
        //
        // 04 - length of port - 4 chars
        // 34303031 - port = ""
        //
        // 01 - length of watchdog - 1 char
        // 36 (hex) - watchdog = 54
        //
        // 03 - baudrate = 9600
        // 00 - bits count = 8 bit
        // 00 - parity = None
        // 00 - stop bits count = 1 bit
        //
        // 26B1 - crc-16

        if (Utils.PacketOfBytes.CheckModbusFunctionAndCrc(bytes, 0x03, this.logger) == ModbusRtuPacketStatus.Success)
        {
            byte currentPos = 3;

            result.DeviceConfig.Apn = StringPropertyHelper.GetValueFromBytes(bytes[currentPos..], typeof(Model.Config), nameof(Model.Config.Apn), this.logger);

            currentPos += bytes[currentPos];
            currentPos++;

            result.DeviceConfig.Login = StringPropertyHelper.GetValueFromBytes(bytes[currentPos..], typeof(Model.Config), nameof(Model.Config.Login), this.logger);

            currentPos += bytes[currentPos];
            currentPos++;

            result.DeviceConfig.Password = StringPropertyHelper.GetValueFromBytes(bytes[currentPos..], typeof(Model.Config), nameof(Model.Config.Password), this.logger,
                modifyFunction: b => (byte)(b ^ 0x55));

            currentPos += bytes[currentPos];
            currentPos++;

            #region Port

            this.logger.LogTrace($"* parse port");

            byte portStrLength = bytes[currentPos];

            if (portStrLength > 5)
            {
                this.logger.LogError("Length {Length} of the port field is more than 5 characters!", portStrLength);

                return;
            }

            ReadOnlySpan<byte> portBytes = bytes.Slice(++currentPos, portStrLength);

            this.logger.LogTrace(":: portBytes [{Bytes}]", Convert.ToHexString(portBytes));

            if (ushort.TryParse(Encoding.ASCII.GetString(portBytes), out ushort portNumber))
            {
                result.DeviceConfig.Port = portNumber;
            }
            else
            {
                this.logger.LogWarning($"* can't convert port bytes to uint!");
            }

            currentPos += portStrLength;

            #endregion

            #region Watchdog

            this.logger.LogTrace($"* parse watchdog");

            byte timeoutStrLength = bytes[currentPos];
            ReadOnlySpan<byte> timeoutBytes = bytes.Slice(++currentPos, timeoutStrLength);

            this.logger.LogTrace(":: timeoutBytes [{Bytes}]", Convert.ToHexString(timeoutBytes));

            if (byte.TryParse(Encoding.ASCII.GetString(timeoutBytes), out byte watchdogTimer))
            {
                result.DeviceConfig.WatchdogTimer = watchdogTimer;
            }
            else
            {
                this.logger.LogWarning($"* can't convert watchdog bytes to byte!");
                result.DeviceConfig.WatchdogTimer = 10;
            }

            result.DeviceConfig.WatchdogTimer = watchdogTimer == 0 ? (byte)10 : watchdogTimer;
            currentPos += timeoutStrLength;

            #endregion

            #region Serial settings

            this.logger.LogTrace($"* parse serialsettings");

            ReadOnlySpan<byte> serialInterfaceBytes = bytes.Slice(currentPos, 4);

            this.logger.LogTrace(":: serialInterfaceBytes [{Bytes}]", Convert.ToHexString(serialInterfaceBytes));

            result.SerialConfig.Baudrate = (RS485Baudrate)serialInterfaceBytes[0];
            result.SerialConfig.BitsCount = (RS485Bits)serialInterfaceBytes[1];
            result.SerialConfig.Parity = (RS485Parity)serialInterfaceBytes[2];
            result.SerialConfig.StopBitsCount = (RS485StopBits)serialInterfaceBytes[3];

            #endregion
        }
    }
}
