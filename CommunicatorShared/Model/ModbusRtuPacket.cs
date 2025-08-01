namespace TMP.Work.CommunicatorPSDTU.Common.Model;

using System;

internal readonly ref struct ModbusRtuPacket
{
    private readonly bool isValid;

    public readonly bool IsValid => this.isValid;

    public readonly string? ErrorMessage { get; }

    public readonly ModbusRtuPacketStatus Status { get; }

    public byte Function { get; }
    public byte Address { get; }

    public ReadOnlySpan<byte> Body { get; }

    public ushort Crc16 { get; }

    public ushort CalculatedCrc16 { get; }

    public ModbusRtuPacket(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            this.isValid = false;
            this.ErrorMessage = "Array of bytes is empty!";
            this.Status = ModbusRtuPacketStatus.NoData;
            return;
        }

        this.Address = data[0];

        if (this.Address != 0x01)
        {
            this.ErrorMessage = $"Address is not equal to 1 - address received '{this.Address}'!";
            this.isValid = false;
            this.Status = ModbusRtuPacketStatus.InvalidAddress;
            return;
        }

        if (data.Length <= 6)
        {
            this.isValid = false;
            this.ErrorMessage = "Array of bytes is too short!";
            this.Status = ModbusRtuPacketStatus.NoData;
            return;
        }
        this.Function = data[1];

        // Number of Bytes
        byte valueLength = data[2];

        this.Body = data.Slice(3, valueLength);

        ReadOnlySpan<byte> valueBytes = data[..^2];

        this.Crc16 = (ushort)((data[^2] << 8) | data[^1]);

        this.CalculatedCrc16 = Utils.CRC.ModbusCrc16(valueBytes);
    }
}
