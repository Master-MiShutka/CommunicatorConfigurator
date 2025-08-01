namespace TMP.Work.CommunicatorPSDTU.Common.Builder;

using System;
using System.Text;
using Microsoft.Extensions.Logging;

public static class RequestBuilder
{
    public static ArraySegment<byte> WriteDeviceConfigRequest(Model.Config deviceConfig, Model.SerialConfig serialConfig, ILogger logger)
    {
        logger.LogTrace("Start preparing a packet of bytes to write a new configuration.");

        const string error = "Non-ASCII characters found in the string being written. String '{Str}'.";

        if (Utils.StringValidator.HasNonASCII(deviceConfig.Apn))
        {
            logger.LogError(error, deviceConfig.Apn);

            throw new ArgumentException(message: error, paramName: deviceConfig.Apn);
        }

        if (Utils.StringValidator.HasNonASCII(deviceConfig.Login))
        {
            logger.LogError(error, deviceConfig.Login);

            throw new ArgumentException(message: error, paramName: deviceConfig.Login);
        }

        if (Utils.StringValidator.HasNonASCII(deviceConfig.Password))
        {
            logger.LogError(error, deviceConfig.Password);

            throw new ArgumentException(message: error, paramName: deviceConfig.Password);
        }

        int currentPosition = 0;
        byte[] buffer = new byte[100];
        Array.Clear(buffer, 0, buffer.Length);

        void writeString(string data)
        {
            Span<byte> bytes = buffer.AsSpan(currentPosition, data.Length);

            int writtedBytesCount = Encoding.ASCII.GetBytes(data, bytes);

            currentPosition += writtedBytesCount;
        }

        void writeByte(byte value) => buffer[currentPosition++] = value;

        void writeInt(int value)
        {
            if (value <= byte.MaxValue)
            {
                buffer[currentPosition++] = (byte)value;
            }
        }

        writeByte(0x01); // адрес
        writeByte(0x10); // Функция. 0x10 – функция записи
        writeByte(0xFC); // Адрес записи Hi. Равен 0xFC для команды «Запись конфигурации устройства»
        writeByte(0x00); // Адрес записи Lo. Равен 0x00 для команды «Запись конфигурации устройства»
        writeByte(0x00); // Количество регистров записи Hi. Значение старшего разряда.
        writeByte(0x0E); // Количество регистров записи Lo. Значение младшего разряда.

        writeByte(0x0); // количество байт далее

        writeInt(deviceConfig.Apn.Length); // длина поля Apn
        writeString(deviceConfig.Apn); // поле Apn

        writeInt(deviceConfig.Login.Length); // длина поля Login
        writeString(deviceConfig.Login); // поле Login

        writeInt(deviceConfig.Password.Length); // длина поля Password
        writeString(deviceConfig.Password); // поле Password

        Span<byte> passwordBytes = buffer.AsSpan(currentPosition - deviceConfig.Password.Length, deviceConfig.Password.Length);
        for (int i = 0; i < passwordBytes.Length; i++)
        {
            passwordBytes[i] = (byte)(passwordBytes[i] ^ 0x55);
        }

        string port = deviceConfig.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        writeInt(port.Length); // длина поля Port
        writeString(port); // поле Port

        string watchdog = deviceConfig.WatchdogTimer.ToString(System.Globalization.CultureInfo.InvariantCulture);
        writeInt(watchdog.Length); // длина поля WatchdogTimer
        writeString(watchdog); // поле WatchdogTimer

        writeByte((byte)serialConfig.Baudrate);
        writeByte((byte)serialConfig.BitsCount);
        writeByte((byte)serialConfig.Parity);
        writeByte((byte)serialConfig.StopBitsCount);

        Span<byte> dataPacketBytes = buffer.AsSpan(7, currentPosition);

        ushort crc16_1 = Utils.CRC.ModbusCrc16(dataPacketBytes);

        Span<byte> crcBytes = buffer.AsSpan(currentPosition, 2);

        BitConverter.TryWriteBytes(crcBytes, crc16_1);
        currentPosition += 2;

        int pos = currentPosition;

        buffer[6] = (byte)(pos - 6 - 2); // количество байт в пакете данных, включая crc16_1, 2 байта crc16_1, 6 байт заголовок пакета

        Span<byte> packetBytes = buffer.AsSpan(0, currentPosition);

        ushort crc16_2 = Utils.CRC.ModbusCrc16(packetBytes);

        crcBytes = buffer.AsSpan(currentPosition, 2);

        BitConverter.TryWriteBytes(crcBytes, crc16_2);
        currentPosition += 2;

        return new ArraySegment<byte>(buffer, 0, currentPosition);
    }
}
