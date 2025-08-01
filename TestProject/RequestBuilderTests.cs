namespace TestProject
{
    using Microsoft.Extensions.Logging;
    using TMP.Work.CommunicatorPSDTU.Common.Builder;
    using TMP.Work.CommunicatorPSDTU.Common.Model;
    using TMP.Work.CommunicatorPSDTU.Common.Parser;

    public class RequestBuilderTests
    {
        private readonly FunctionsAnswerParser functionsAnswerParser;
        private readonly ILogger logger;

        public RequestBuilderTests()
        {
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddDebug());

            this.logger = loggerFactory.CreateLogger("Main");

            this.functionsAnswerParser = new FunctionsAnswerParser(this.logger);
        }

        [Fact]
        public void TestWriteDeviceConfigRequest1()
        {
            (Config DeviceConfig, SerialConfig RS485Config) config = new(new(), new());
            config.DeviceConfig.Apn = "apn.operator.cpm";
            config.DeviceConfig.Login = "vpn";
            config.DeviceConfig.Password = "admin12345";
            config.DeviceConfig.Port = 4001;
            config.DeviceConfig.WatchdogTimer = 10;
            config.RS485Config.Baudrate = RS485Baudrate.Rate9600;
            config.RS485Config.BitsCount = RS485Bits.Eight;
            config.RS485Config.Parity = RS485Parity.Odd;
            config.RS485Config.StopBitsCount = RS485StopBits.One;

            ArraySegment<byte> resultBytes = RequestBuilder.WriteDeviceConfigRequest(config.DeviceConfig, config.RS485Config, this.logger);

            string resultBytesAsHex = Convert.ToHexString(resultBytes);

            Assert.Equal("0110FC00000E2D1061706E2E6F70657261746F722E63706D0376706E0A3431383C3B64676661600434303031023130030002001B71DB6B", resultBytesAsHex);
        }

        [Fact]
        public void TestWriteDeviceConfigRequestWithUnicodeData()
        {
            (Config DeviceConfig, SerialConfig RS485Config) config = new(new(), new());
            config.DeviceConfig.Apn = "apn.operator.cpm";
            config.DeviceConfig.Login = "vpn😒";
            config.DeviceConfig.Password = "admin12345";
            config.DeviceConfig.Port = 4001;
            config.DeviceConfig.WatchdogTimer = 10;
            config.RS485Config.Baudrate = RS485Baudrate.Rate9600;
            config.RS485Config.BitsCount = RS485Bits.Eight;
            config.RS485Config.Parity = RS485Parity.Odd;
            config.RS485Config.StopBitsCount = RS485StopBits.One;

            Assert.Throws<ArgumentException>(() => RequestBuilder.WriteDeviceConfigRequest(config.DeviceConfig, config.RS485Config, this.logger));
        }

        [Fact]
        public void TestWriteDeviceConfigRequestWithWrongTextFieldLength()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                (Config DeviceConfig, SerialConfig RS485Config) config = new(new(), new());
                config.DeviceConfig.Apn = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                config.DeviceConfig.Login = "bbbbbbbbbbbbbbbbb";
                config.DeviceConfig.Password = "ccccccccccccccccccc";
                config.DeviceConfig.Port = 4001;
                config.DeviceConfig.WatchdogTimer = 10;
                config.RS485Config.Baudrate = RS485Baudrate.Rate9600;
                config.RS485Config.BitsCount = RS485Bits.Eight;
                config.RS485Config.Parity = RS485Parity.Odd;
                config.RS485Config.StopBitsCount = RS485StopBits.One;
                RequestBuilder.WriteDeviceConfigRequest(config.DeviceConfig, config.RS485Config, this.logger);
            });
        }

        [Fact]
        public void TestWriteDeviceConfigRequestWithWrongTextField()
        {
            (Config DeviceConfig, SerialConfig RS485Config) config = new(new(), new());
            config.DeviceConfig.Apn = "";
            config.DeviceConfig.Login = "";
            config.DeviceConfig.Password = "";
            config.DeviceConfig.Port = 4001;
            config.DeviceConfig.WatchdogTimer = 10;
            config.RS485Config.Baudrate = RS485Baudrate.Rate9600;
            config.RS485Config.BitsCount = RS485Bits.Eight;
            config.RS485Config.Parity = RS485Parity.Odd;
            config.RS485Config.StopBitsCount = RS485StopBits.One;

            ArraySegment<byte> resultBytes = RequestBuilder.WriteDeviceConfigRequest(config.DeviceConfig, config.RS485Config, this.logger);

            string resultBytesAsHex = Convert.ToHexString(resultBytes);

            Assert.Equal("0110FC00000E10000000043430303102313003000200DD03F7E0", resultBytesAsHex);
        }

        [Theory]
        [InlineData("0103260B76706E322E6D74732E62790376706E0A3226316C3127303E3F60043430303101360300000026B1")]
        public void TestWriteDeviceConfigRequest2(string bytesAsHexString)
        {
            byte[] configAsBytes = Convert.FromHexString(bytesAsHexString);

            (Config DeviceConfig, SerialConfig RS485Config) config = new(new(), new());

            this.functionsAnswerParser.ParseConfig(configAsBytes, ref config);

            ArraySegment<byte> resultBytes = RequestBuilder.WriteDeviceConfigRequest(config.DeviceConfig, config.RS485Config, this.logger);

            string resultBytesAsHex = Convert.ToHexString(resultBytes);

            Assert.Equal("0110FC00000E270B76706E322E6D74732E62790376706E0A3226316C3127303E3F600434303031013603000000B3907E85", resultBytesAsHex);
        }

        [Theory]
        [InlineData("0103260B76706E322E6D74732E62790376706E0A3226316C3127303E3F60043430303101360300000026B1")]
        public void TestWriteDeviceConfigRequest3(string bytesAsHexString)
        {
            byte[] configAsBytes = Convert.FromHexString(bytesAsHexString);

            (Config DeviceConfig, SerialConfig RS485Config) config = new(new(), new());

            this.functionsAnswerParser.ParseConfig(configAsBytes, ref config);

            ArraySegment<byte> resultBytes = RequestBuilder.WriteDeviceConfigRequest(config.DeviceConfig, config.RS485Config, this.logger);

            string resultBytesAsHex = Convert.ToHexString(resultBytes);

            Assert.Equal("0110FC00000E270B76706E322E6D74732E62790376706E0A3226316C3127303E3F600434303031013603000000B3907E85", resultBytesAsHex);
        }
    }
}
