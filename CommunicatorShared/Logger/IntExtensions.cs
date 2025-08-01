namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

internal static class IntExtensions
{

    /// <summary>
    /// This is a compute-optimized function that returns a number of decimal digets of the specified int value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    internal static int GetFormattedLength(this int value)
    {
        if (value == 0)
        {
            return 1; // fast result for typical EventId value (0)
        }

        uint absVal;
        int signLen = 0;
        if (value < 0)
        {
            absVal = ((uint)~value) + 1; // abs value of two's comlpement signed integer
            signLen = 1;
        }
        else
        {
            absVal = (uint)value;
        }

        return absVal switch
        {
            < 10 => 1 + signLen,
            < 100 => 2 + signLen,
            < 1000 => 3 + signLen,
            < 10000 => 4 + signLen,
            < 100000 => 5 + signLen,
            < 1000000 => 6 + signLen,
            < 10000000 => 7 + signLen,
            < 100000000 => 8 + signLen,
            < 1000000000 => 9 + signLen,
            _ => 10 + signLen
        };
    }
}
