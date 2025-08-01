namespace TMP.Work.CommunicatorPSDTU.Common.Model
{
    public enum RS485Bits : byte
    {
        [LocalizedDescriptionAttribute(ResourceKey: "EightBits", ResourceType: typeof(Resources.PropertiesNames))]
        Eight = 0,
        [LocalizedDescriptionAttribute(ResourceKey: "SevenBits", ResourceType: typeof(Resources.PropertiesNames))]
        Seven = 1
    }
}
