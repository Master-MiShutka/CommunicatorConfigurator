namespace TestProject
{
    using Microsoft.Extensions.Logging;
    using TMP.Work.CommunicatorPSDTU.Common.Model;
    using TMP.Work.CommunicatorPSDTU.Common.Parser;
    using TMP.Work.CommunicatorPSDTU.Common.ViewModels;

    public class MainViewModelTests : IDisposable
    {
        private readonly FunctionsAnswerParser functionsAnswerParser;
        private readonly MainViewModel mainViewModel;

        private readonly byte[] infoAnswerOk, netModeAndNetLevelOk, netConfigOk, configOk;

        public MainViewModelTests()
        {
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddDebug());

            var logger = loggerFactory.CreateLogger("Main");

            this.functionsAnswerParser = new FunctionsAnswerParser(logger);
            this.mainViewModel = new MainViewModel(logger);

            // "+Communicator GPRS-3G-LTE FirmWare:07.130523+?"
            this.infoAnswerOk = Convert.FromHexString("01112B436F6D6D756E696361746F7220475052532D33472D4C5445204669726D576172653A30372E3133303532332B90");

            this.netModeAndNetLevelOk = Convert.FromHexString("010302800F9980");

            this.netConfigOk = Convert.FromHexString("010304000C1A003150");

            this.configOk = Convert.FromHexString("0103260B76706E322E6D74732E62790376706E0A3226316C3127303E3F60043430303101360300000026B1");
        }

        ~MainViewModelTests()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.mainViewModel.Dispose();

            GC.SuppressFinalize(this);
        }

        [Fact(DisplayName = "Проверка разбора массива байт с описанием устройства - имеется название.")]
        public void TestParseDeviceName()
        {
            Info info = new();

            this.functionsAnswerParser.ParseInfo(this.infoAnswerOk, ref info);

            Assert.False(string.IsNullOrEmpty(info.Name), "Название не должно быть пустым!");
        }

        [Fact(DisplayName = "Проверка разбора массива байт с описанием устройства - верное название.")]
        public void TestParseDeviceNameValue()
        {
            Info info = new();

            this.functionsAnswerParser.ParseInfo(this.infoAnswerOk, ref info);

            Assert.True(info.Name == "Communicator GPRS-3G-LTE", "Ошибка разбора названия!");
        }

        [Fact(DisplayName = "Проверка разбора массива байт с описанием устройства - номер прошивки.")]
        public void TestParseDeviceFirmwareVersion()
        {
            Info info = new();

            this.functionsAnswerParser.ParseInfo(this.infoAnswerOk, ref info);

            Assert.True(info.FirmwareVersion == "07", "Ошибка разбора версии прошивки!");
        }

        [Fact(DisplayName = "Проверка разбора массива байт с описанием устройства - дата прошивки.")]
        public void TestParseDeviceFirmwareDate()
        {
            Info info = new();

            this.functionsAnswerParser.ParseInfo(this.infoAnswerOk, ref info);

            Assert.True(info.FirmwareDate == "130523", "Ошибка разбора версии прошивки!");
        }

        [Fact(DisplayName = "Проверка разбора массива байт - тип сети и уровень.")]
        public void TestParseNetModeAndNetLevel()
        {
            (string netMode, byte netLevel) result = (string.Empty, 0);

            this.functionsAnswerParser.ParseNetworkModeAndLevel(this.netModeAndNetLevelOk, ref result);

            Assert.True(result.netMode == "3G", "Ошибка разбора режима сети!");

            Assert.True(result.netLevel == 15, "Ошибка разбора уровня сети!");
        }

        [Fact(DisplayName = "Проверка разбора массива байт - состояния сети.")]
        public void TestParseNetConfig()
        {
            NetworkConfig result = new();

            this.functionsAnswerParser.ParseNetworkConfig(this.netConfigOk, ref result);

            Assert.True(result.NetworkFrequency == "1800 MHz, 4G; 2100 MHz, 3G; 1800 MHz, 2G", "Ошибка разбора состояния сети!");

            Assert.True(result.NetworkType == "Auto", "Ошибка разбора типа сети!");

            Assert.True(result.NetworkPriority == "4G/3G/2G", "Ошибка разбора приоритета сети!");
        }

        [Fact(DisplayName = "Проверка разбора массива байт - настройки устройства.")]
        public void TestParseDeviceConfig()
        {
            (Config DeviceConfig, SerialConfig RS485Config) result = new(new(), new());

            this.functionsAnswerParser.ParseConfig(this.configOk, ref result);

            Assert.True(result.DeviceConfig.Apn == "vpn2.mts.by", "Ошибка разбора названия мобильной точки доступа!");

            Assert.True(result.DeviceConfig.Login == "vpn", "Ошибка разбора имени пользователя мобильной точки доступа!");

            Assert.True(result.DeviceConfig.Password == "gsd9drekj5", "Ошибка разбора пароля мобильной точки доступа!");

            Assert.True(result.DeviceConfig.Port == 4001, "Ошибка разбора TCP порта!");

            Assert.True(result.DeviceConfig.WatchdogTimer == 6, "Ошибка разбора таймера whatchdog!");

            Assert.True(result.RS485Config.Baudrate == RS485Baudrate.Rate9600, "Ошибка разбора настроек последовательного порта - скорость!");
            Assert.True(result.RS485Config.BitsCount == RS485Bits.Eight, "Ошибка разбора настроек последовательного порта - бит данных!");
            Assert.True(result.RS485Config.Parity == RS485Parity.None, "Ошибка разбора настроек последовательного порта - паритет!");
            Assert.True(result.RS485Config.StopBitsCount == RS485StopBits.One, "Ошибка разбора настроек последовательного порта - стоп-биты!");
        }
    }
}
