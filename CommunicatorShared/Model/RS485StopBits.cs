namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    public enum RS485StopBits : byte
    {
        [LocalizedDescriptionAttribute(ResourceKey: "StopBitsOne", ResourceType: typeof(Resources.PropertiesNames))]
        One = 0,

        [LocalizedDescriptionAttribute(ResourceKey: "StopBitsTwo", ResourceType: typeof(Resources.PropertiesNames))]
        Two = 1
    }
}
