namespace TestProject;

using TMP.Work.CommunicatorPSDTU.Common.Model;

public class ModbusRtuPacketTests
{
    public ModbusRtuPacketTests()
    {

    }

    [Fact]
    public void TestIsNotValid()
    {
        string bytes = "01112F436F6D6D756E696361746F7220475052532D33472D4C54455F534C4D204669726D576172653A30382E3132313232336C06";

        byte[] data = Convert.FromHexString(bytes);

        ModbusRtuPacket packet = new ModbusRtuPacket(data);

        Assert.False(packet.IsValid);
    }

    [Fact]
    public void TestIsValid()
    {
        string bytes = "01112B436F6D6D756E696361746F7220475052532D33472D4C5445204669726D576172653A30372E3133303532332B90";

        byte[] data = Convert.FromHexString(bytes);

        ModbusRtuPacket packet = new ModbusRtuPacket(data);

        Assert.True(packet.IsValid);
    }
}
