namespace TMP.Work.CommunicatorPSDTU.Common.Utils;

using System;

public static class CRC
{
    /// <summary>
    /// Расчёт контрольной суммы массива байт используя алгоритм Modbus RTU Message CRC16
    /// </summary>
    /// <param name="bytes">массив байт</param>
    /// <returns>CRC16</returns>
    public static ushort ModbusCrc16(ReadOnlySpan<byte> bytes)
    {
        ushort crc = 0xFFFF;
        int len = bytes.Length;

        for (int pos = 0; pos < len; pos++)
        {
            crc ^= bytes[pos];

            for (int i = 8; i != 0; i--)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc >>= 1;
                    crc ^= 0xA001;
                }
                else
                {
                    crc >>= 1;
                }
            }
        }

        // lo-hi
        //return crc;

        // ..or
        // hi-lo reordered
        return (ushort)((crc >> 8) | (crc << 8));
    }
}
