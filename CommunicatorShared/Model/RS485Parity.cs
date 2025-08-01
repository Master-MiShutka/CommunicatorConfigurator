namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    public enum RS485Parity : byte
    {
        [LocalizedDescriptionAttribute(ResourceKey: "ParityNone", ResourceType: typeof(Resources.PropertiesNames))]
        None = 0,
        [LocalizedDescriptionAttribute(ResourceKey: "ParityEven", ResourceType: typeof(Resources.PropertiesNames))]
        Even = 1,
        [LocalizedDescriptionAttribute(ResourceKey: "ParityOdd", ResourceType: typeof(Resources.PropertiesNames))]
        Odd = 2
    }
}
