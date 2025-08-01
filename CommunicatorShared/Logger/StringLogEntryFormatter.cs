namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;
using System.Buffers;
using System.Text;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements low-allocation default log entry formatting
/// </summary>
internal class StringLogEntryFormatter
{
    internal static readonly StringLogEntryFormatter Instance = new();

    public StringLogEntryFormatter()
    {
    }

    string GetShortLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "FAIL",
            LogLevel.Critical => "CRIT",
            LogLevel.None => "NONE",
            _ => logLevel.ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture),
        };
    }

    /// <summary>
    /// This is a StringBuilder-based tab-separated log entry formatter (reference implementation).
    /// </summary>
    public string StringBuilderLogEntryFormat(string logName, DateTime timeStamp, LogLevel logLevel, EventId eventId, string? message, Exception? exception)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(message))
        {
            sb.Append(timeStamp.ToString("o"));
            sb.Append('\t');
            sb.Append(this.GetShortLogLevel(logLevel));
            sb.Append("\t[");
            sb.Append(logName);
            sb.Append(']');
            sb.Append("\t[");
            sb.Append(eventId);
            sb.Append("]\t");
            sb.Append(message);
        }

        if (exception != null)
        {
            // exception message
            sb.AppendLine(exception.ToString());
        }
        return sb.ToString();
    }

    public string StringBuilderLogEntryFormat(LogMessage logMsg, DateTime timeStamp)
        => this.StringBuilderLogEntryFormat(logMsg.LogName, timeStamp, logMsg.LogLevel, logMsg.EventId, logMsg.Message, logMsg.Exception);

    /// <summary>
    /// This is a low-allocation optimized tab-separated log entry formatter that formats output identical to <see cref="StringBuilderLogEntryFormat"/>
    /// </summary>
    public string LowAllocLogEntryFormat(string logName, DateTime timeStamp, LogLevel logLevel, EventId eventId, string? message, Exception? exception)
    {
        const int maxStackAllocatedBufferLength = 256;
        var logMessageLength = CalculateLogMessageLength();
        char[]? charBuffer = null;
        try
        {
            Span<char> buffer = logMessageLength <= maxStackAllocatedBufferLength
                ? stackalloc char[maxStackAllocatedBufferLength]
                : (charBuffer = ArrayPool<char>.Shared.Rent(logMessageLength));

            // default formatting logic
            using var logBuilder = new ValueStringBuilder(buffer);
            if (!string.IsNullOrEmpty(message))
            {
                timeStamp.TryFormatO(logBuilder.RemainingRawChars, out var charsWritten);
                logBuilder.AppendSpan(charsWritten);
                logBuilder.Append('\t');
                logBuilder.Append(this.GetShortLogLevel(logLevel));
                logBuilder.Append("\t[");
                logBuilder.Append(logName);
                logBuilder.Append("]\t[");
                if (eventId.Name is not null)
                {
                    logBuilder.Append(eventId.Name);
                }
                else
                {
                    eventId.Id.TryFormat(logBuilder.RemainingRawChars, out charsWritten, provider: System.Globalization.CultureInfo.InvariantCulture);
                    logBuilder.AppendSpan(charsWritten);
                }
                logBuilder.Append("]\t");
                logBuilder.Append(message);
            }

            if (exception != null)
            {
                // exception message
                logBuilder.Append(exception.ToString());
                logBuilder.Append(Environment.NewLine);
            }
            return logBuilder.ToString();
        }
        finally
        {
            if (charBuffer is not null)
            {
                ArrayPool<char>.Shared.Return(charBuffer);
            }
        }

        int CalculateLogMessageLength() => timeStamp.GetFormattedLength()
                + 1 /* '\t' */
                + 4 /* GetShortLogLevel */
                + 2 /* "\t[" */
                + (logName?.Length ?? 0)
                + 3 /* "]\t[" */
                + (eventId.Name?.Length ?? eventId.Id.GetFormattedLength())
                + 2 /* "]\t" */
                + (message?.Length ?? 0);
    }

    public string LowAllocLogEntryFormat(LogMessage logMsg, DateTime timeStamp)
        => this.LowAllocLogEntryFormat(logMsg.LogName, timeStamp, logMsg.LogLevel, logMsg.EventId, logMsg.Message, logMsg.Exception);
}
