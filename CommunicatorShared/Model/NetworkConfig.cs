namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    using System.Diagnostics;

    [DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
    public partial class NetworkConfig : ObservableObject
    {
        [ObservableProperty] public partial string NetworkType { get; set; } = string.Empty;

        [ObservableProperty] public partial string NetworkPriority { get; set; } = string.Empty;

        [ObservableProperty] public partial string NetworkFrequency { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{this.NetworkType}; {this.NetworkFrequency}; {this.NetworkPriority}.";
        }

        private string GetDebuggerDisplay() => this.ToString();
    }
}
