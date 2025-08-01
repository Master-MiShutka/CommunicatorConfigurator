namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Конфигурация коммуникатора
    /// </summary>
    public partial class Config : ObservableValidator, IEquatable<Config>, ICloneable, IDataErrorInfo
    {
        /// <summary>
        /// Название мобильной точки доступа. Максимальная длина поля – 32 символа
        /// </summary>
        [MaxLength(32, ErrorMessageResourceName = "MaxLengthHasBeenExceeded32", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [NotifyDataErrorInfo]
        [Display(Name = "ApnProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial string Apn { get; set; } = string.Empty;

        /// <summary>
        /// Имя пользователя мобильной точки доступа. Максимальная длина поля – 16 символов
        /// </summary>
        [MaxLength(16, ErrorMessageResourceName = "MaxLengthHasBeenExceeded16", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [NotifyDataErrorInfo]
        [Display(Name = "LoginProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial string Login { get; set; } = string.Empty;

        /// <summary>
        /// Пароль мобильной точки доступа. Символы кодируются маской XOR 55 (HEX). Максимальная длина поля – 16 символов
        /// </summary>
        [MaxLength(16, ErrorMessageResourceName = "MaxLengthHasBeenExceeded16", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [NotifyDataErrorInfo]
        [Display(Name = "PasswordProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial string Password { get; set; } = string.Empty;

        /// <summary>
        /// TCP порт (используется для подключения к устройству)
        /// </summary>
        [NotifyDataErrorInfo]
        [Required(ErrorMessageResourceName = "ItIsARequiredField", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [Range(1001, 65535, ErrorMessageResourceName = "PortValueValidationMessage", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [Display(Name = "PortProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial ushort Port { get; set; } = 1001;

        /// <summary>
        /// Сторожевой таймер в минутах
        /// </summary>
        [NotifyDataErrorInfo]
        [Range(1, 99, ErrorMessageResourceName = "WatchdogTimerValueValidationMessage", ErrorMessageResourceType = typeof(Resources.ValidatingErrors))]
        [Display(Name = "WatchdogTimerProperty", ResourceType = typeof(Resources.PropertiesNames))]
        [ObservableProperty] public partial byte WatchdogTimer { get; set; } = 30;

        #region overrides
        public override string ToString()
        {
            return $"APN:'{this.Apn}', '{this.Login}':'{this.Password}', port:{this.Port}, watchdog: {this.WatchdogTimer}";
        }

        public override int GetHashCode() => HashCode.Combine(this.Apn, this.Login, this.Password, this.Port, this.WatchdogTimer);

        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is not null and Config otherConfig) && this.Equals(otherConfig);

        #endregion

        #region IEquatable implementation
        public bool Equals(Config? other) => this.GetHashCode() == other?.GetHashCode();
        public static bool operator ==(Config? left, Config? right) => left?.Equals(right) ?? false;

        public static bool operator !=(Config? left, Config? right) => !(left == right);

        #endregion

        #region ICloneable implementation

        public object Clone()
        {
            return new Config()
            {
                Apn = this.Apn,
                Login = this.Login,
                Password = this.Password,
                Port = this.Port,
                WatchdogTimer = this.WatchdogTimer,
            };
        }

        public Config CloneConfig() => (Config)this.Clone();

        #endregion

        #region IDataErrorInfo implementation

        public string Error { get; } = string.Empty;

        public string this[string columnName]
        {
            get
            {
                string error = string.Empty;
                bool containsNonASCII = false;
                switch (columnName)
                {
                    case nameof(this.Apn):
                        containsNonASCII = Utils.StringValidator.HasNonASCII(this.Apn);
                        break;
                    case nameof(this.Login):
                        containsNonASCII = Utils.StringValidator.HasNonASCII(this.Login);
                        break;
                    case nameof(this.Password):
                        containsNonASCII = Utils.StringValidator.HasNonASCII(this.Password);
                        break;
                    case nameof(this.Port):
                        if (this.Port <= 1000)
                        {
                            error = "Значения меньше 1 000 не рекомендуется.";
                        }
                        break;
                    default:
                        //Обработка ошибок для свойства
                        break;
                }

                if (containsNonASCII)
                {
                    error = "Поле содержит символы в неподдерживаемой кодировке!";
                }

                return error;
            }
        }

        #endregion
    }
}
