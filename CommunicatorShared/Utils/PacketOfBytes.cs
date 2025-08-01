namespace TMP.Work.CommunicatorPSDTU.Common.Utils;

using System;
using Microsoft.Extensions.Logging;
using TMP.Work.CommunicatorPSDTU.Common.Model;

internal static class PacketOfBytes
{
    public static ModbusRtuPacketStatus CheckModbusFunctionAndCrc(ReadOnlySpan<byte> bytes, byte expectedFunctionNumber, ILogger logger)
    {
        ModbusRtuPacket packet = new ModbusRtuPacket(bytes);

        if (packet.IsValid == false)
        {
            return packet.Status;
        }
        else
        {
            if (packet.Function != expectedFunctionNumber)
            {
                logger.LogError("Unknown function - expected number {ExpectedFunctionNumber}, number received {ReceivedFunctionNumber}!", expectedFunctionNumber, packet.Function);

                return ModbusRtuPacketStatus.UnknownFunction;
            }

            if (packet.Crc16 != packet.CalculatedCrc16)
            {
                logger.LogError("The checksum of the data packet does not match! In the packet {CrcPacket}, and the disheveled one {CrcCalculated:X2}.", packet.Crc16, packet.CalculatedCrc16);

                return ModbusRtuPacketStatus.WrongCrc;
            }
        }

        return ModbusRtuPacketStatus.Success;
    }
}
