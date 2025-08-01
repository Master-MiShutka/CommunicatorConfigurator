namespace TMP.Work.CommunicatorPSDTU.Common.Utils;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

/// <summary>
/// https://stackoverflow.com/questions/479410/enum-tostring-with-user-friendly-strings
/// </summary>
public static class Common
{
    public static string GetDescription<T>(this T enumerationValue)
    where T : struct
    {
        Type type = enumerationValue.GetType();
        if (!type.IsEnum)
        {
            throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
        }

        //Tries to find a DescriptionAttribute for a potential friendly name
        //for the enum
        MemberInfo[] memberInfo = type.GetMember(enumerationValue.ToString() ?? string.Empty);
        if (memberInfo != null && memberInfo.Length > 0)
        {
            object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrs != null && attrs.Length > 0)
            {
                //Pull out the description value
                return ((DescriptionAttribute)attrs[0]).Description;
            }
        }

        //If we have no description attribute, just return the ToString of the enum
        return enumerationValue.ToString() ?? "???";
    }

    public static string GetExceptionDetails(Exception? exp)
    {
        string message = string.Empty;

        if (exp == null)
        {
            return message;
        }

        if (exp is AggregateException)
        {
            if (exp is not AggregateException ae)
            {
                return message;
            }

            foreach (var e in ae.InnerExceptions)
            {
                message += Environment.NewLine + e.Message + Environment.NewLine;
            }

            ParseException(ae.InnerExceptions.LastOrDefault());
        }
        else
        {
            ParseException(exp);
        }

        message += "\n" + BuildStackTrace(exp);

        void ParseException(Exception? e)
        {
            if (e == null)
            {
                return;
            }

            try
            {
                // Write Message tree of inner exception into textual representation
                message = e.Message;

                if (e.InnerException == null)
                {
                    return;
                }

                Exception? innerEx = e.InnerException;

                for (int i = 0; innerEx != null; i++, innerEx = innerEx.InnerException)
                {
                    string spaces = string.Empty;

                    for (int j = 0; j < i; j++)
                    {
                        spaces += "  ";
                    }

                    message += "\n" + spaces + "└─>" + innerEx.Message;
                }
            }
            catch
            {
            }
        }

        return message;
    }

    public static string BuildStackTrace(Exception e)
    {
        var st = new StackTrace(e, true);
        var frames = st.GetFrames();
        var traceString = new StringBuilder();

        foreach (var frame in frames)
        {
            if (frame.GetFileLineNumber() < 0)
            {
                continue;
            }

            var str = $"file: {frame.GetFileName()}, method:{frame.GetMethod()?.Name ?? "<?>"},  lineNumber:{frame.GetFileLineNumber()};\n";
            traceString = traceString.Append(str);
        }

        return traceString.ToString();
    }
}
