namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using Utils;

    /// <summary>
    /// Настройки последовательного порта (RS485)
    /// </summary>
    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public partial class SerialConfig : ObservableObject, IEquatable<SerialConfig>, ICloneable
    {
        /// <summary>
        /// Скорость передачи данных
        /// </summary>
        [ObservableProperty] public partial RS485Baudrate Baudrate { get; set; } = RS485Baudrate.Rate9600;

        /// <summary>
        /// Количество бит данных
        /// </summary>
        [ObservableProperty] public partial RS485Bits BitsCount { get; set; } = RS485Bits.Eight;

        /// <summary>
        /// Количество стоповых бит
        /// </summary>
        [ObservableProperty] public partial RS485StopBits StopBitsCount { get; set; } = RS485StopBits.One;

        /// <summary>
        /// Паритет
        /// </summary>
        [ObservableProperty] public partial RS485Parity Parity { get; set; } = RS485Parity.None;

        public override string ToString()
        {
            return $"{this.Baudrate.GetDescription()}:{this.BitsCount.GetDescription()}:{this.Parity.GetDescription()}:{this.StopBitsCount.GetDescription()}";
        }

        private string GetDebuggerDisplay() => this.ToString();

        #region overrides

        public override int GetHashCode() => HashCode.Combine(this.Baudrate, this.BitsCount, this.StopBitsCount, this.Parity);

        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is not null and SerialConfig otherConfig) && this.Equals(otherConfig);

        #endregion

        #region IEquatable implementation
        public bool Equals(SerialConfig? other) => this.GetHashCode() == other?.GetHashCode();

        public static bool operator ==(SerialConfig? left, SerialConfig? right) => left?.Equals(right) ?? false;

        public static bool operator !=(SerialConfig? left, SerialConfig? right) => !(left == right);

        #endregion

        #region ICloneable implementation

        public object Clone()
        {
            return new SerialConfig()
            {
                Baudrate = this.Baudrate,
                BitsCount = this.BitsCount,
                StopBitsCount = this.StopBitsCount,
                Parity = this.Parity
            };
        }

        public SerialConfig CloneConfig() => (SerialConfig)this.Clone();

        #endregion
    }
}
