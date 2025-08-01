namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;

internal static class DateTimeExtensions
{
    internal static int GetFormattedLength(this DateTime dateTime)
    {
        const int baseCharCountInFormatO = 27;

        return baseCharCountInFormatO + dateTime.Kind switch
        {
            DateTimeKind.Local => 6,
            DateTimeKind.Utc => 1,
            DateTimeKind.Unspecified => throw new NotImplementedException(),
            _ => 0
        };
    }

    internal static bool TryFormatO(this DateTime dateTime, Span<char> destination, out int charsWritten)
    {
        return dateTime.TryFormat(destination, out charsWritten, format: "O", System.Globalization.CultureInfo.InvariantCulture);
    }
}
