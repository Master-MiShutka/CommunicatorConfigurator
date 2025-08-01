namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Customized ILogger, writes logs to text files
/// </summary>
public sealed class TextFileLogger(string categoryName, TextFileLoggerProvider textFileLoggerProvider) : ILogger
{
    private readonly string categoryName = categoryName;
    private readonly TextFileLoggerProvider textFileLoggerProvider = textFileLoggerProvider;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Ensure that only information level and higher logs are recorded
        return logLevel >= this.textFileLoggerProvider.MinLevel;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Ensure that only information level and higher logs are recorded
        if (!this.IsEnabled(logLevel))
        {
            return;
        }

        // Get the formatted log message
        var message = formatter(state, exception);

        if (this.textFileLoggerProvider.FilterLogEntry != null)
        {
            if (!this.textFileLoggerProvider.FilterLogEntry(new LogMessage(this.textFileLoggerProvider.LogFileName, logLevel, eventId, message, exception)))
            {
                return;
            }
        }

        if (this.textFileLoggerProvider.FormatLogEntry != null)
        {
            this.textFileLoggerProvider.WriteEntry(this.textFileLoggerProvider.FormatLogEntry(
                new LogMessage(this.textFileLoggerProvider.LogFileName, logLevel, eventId, message, exception)));
        }
        else
        {
            this.textFileLoggerProvider.WriteEntry(
                StringLogEntryFormatter.Instance.LowAllocLogEntryFormat(
                    this.textFileLoggerProvider.LogFileName,
                    DateTime.UtcNow,
                    logLevel,
                    eventId,
                    message,
                    exception));
        }
    }
}
