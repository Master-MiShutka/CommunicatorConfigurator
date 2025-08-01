namespace TMP.Work.CommunicatorPSDTU.Common.Utils;

using System.Text.RegularExpressions;

public static class StringValidator
{
    public static bool HasNonASCII(string input)
    {
        const string pattern = @"[^\x00-\x7F]+";

        RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline;

        return Regex.IsMatch(input, pattern, options);
    }
}
