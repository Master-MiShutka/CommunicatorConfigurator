namespace TMP.Work.CommunicatorPSDTU.Common.Model;

public enum ModbusRtuPacketStatus
{
    Success,
    NoData,
    InvalidAddress,
    UnknownFunction,
    WrongCrc
}
