namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    public enum RS485Baudrate : byte
    {
        [System.ComponentModel.Description("1200")]
        Rate1200 = 1,
        [System.ComponentModel.Description("4800")]
        Rate4800 = 2,
        [System.ComponentModel.Description("9600")]
        Rate9600 = 3,
        [System.ComponentModel.Description("19200")]
        Rate19200 = 4,
        [System.ComponentModel.Description("38400")]
        Rate38400 = 5,
        [System.ComponentModel.Description("57600")]
        Rate57600 = 6,
        [System.ComponentModel.Description("115200")]
        Rate115200 = 7
    }
}
