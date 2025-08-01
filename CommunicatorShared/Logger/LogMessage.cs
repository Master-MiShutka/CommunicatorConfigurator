namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;
using Microsoft.Extensions.Logging;

public readonly struct LogMessage
{
    public readonly string LogName { get; }
    public readonly string Message { get; }
    public readonly LogLevel LogLevel { get; }
    public readonly EventId EventId { get; }
    public readonly Exception? Exception { get; }

    internal LogMessage(string logName, LogLevel level, EventId eventId, string message, Exception? ex)
    {
        this.LogName = logName;
        this.Message = message;
        this.LogLevel = level;
        this.EventId = eventId;
        this.Exception = ex;
    }
}
