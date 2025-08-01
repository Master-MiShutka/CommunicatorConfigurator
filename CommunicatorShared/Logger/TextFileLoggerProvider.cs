namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;

[System.Diagnostics.CodeAnalysis.SuppressMessage("PropertyChangedAnalyzers.PropertyChanged", "INPC001:The class has mutable properties and should implement INotifyPropertyChanged.", Justification = "<Pending>")]
public class TextFileLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TextFileLogger> loggers = new();

    private readonly BlockingCollection<string> entryQueue = new(10240);
    private readonly Task processQueueTask;
    private readonly FileWriter fileWriter;

    public TextFileLoggerProvider(string fileName)
    {
        this.LogFileName = Environment.ExpandEnvironmentVariables(fileName);

        this.fileWriter = new FileWriter(this);
        this.processQueueTask = Task.Factory.StartNew(
            action: this.ProcessQueue,
            creationOptions: TaskCreationOptions.LongRunning);
    }

    public ILogger CreateLogger(string categoryName)
    {
        return this.loggers.GetOrAdd(categoryName, this.CreateLoggerImplementation);
    }

    private TextFileLogger CreateLoggerImplementation(string name)
    {
        return new TextFileLogger(name, this);
    }

    internal void WriteEntry(string message)
    {
        if (!this.entryQueue.IsAddingCompleted)
        {
            try
            {
                this.entryQueue.Add(message);
                return;
            }
            catch (InvalidOperationException) { }
        }
        // do nothing
    }

    private void ProcessQueue()
    {
        var writeMessageFailed = false;
        foreach (var message in this.entryQueue.GetConsumingEnumerable())
        {
            try
            {
                if (!writeMessageFailed)
                {
                    this.fileWriter.WriteMessage(message, this.entryQueue.Count == 0);
                }
            }
            catch (Exception ex)
            {
                // something goes wrong. App's code can handle it if 'HandleFileError' is provided
                var stopLogging = true;
                if (this.HandleFileError != null)
                {
                    var logFileError = new LogFileError(this.LogFileName, ex);
                    try
                    {
                        this.HandleFileError(logFileError);
                        if (logFileError.NewLogFileName != null)
                        {
                            this.fileWriter.UseNewLogFile(logFileError.NewLogFileName);
                            // write failed message to a new log file
                            this.fileWriter.WriteMessage(message, this.entryQueue.Count == 0);
                            stopLogging = false;
                        }
                    }
                    catch
                    {
                        // exception is possible in HandleFileError or if proposed file name cannot be used
                        // let's ignore it in that case -> file logger will stop processing log messages
                    }
                }
                if (stopLogging)
                {
                    // Stop processing log messages since they cannot be written to a log file
                    this.entryQueue.CompleteAdding();
                    writeMessageFailed = true;
                }
            }
        }
    }

    private static void ProcessQueue(object state)
    {
        var fileLogger = (TextFileLoggerProvider)state;
        fileLogger.ProcessQueue();
    }

    #region Public properties

    public string LogFileName { get; internal set; } = "log.log";

    /// <summary>
    /// Custom formatter for the log file name
    /// </summary>
    /// <remarks>By specifying custom formatting handler you can define your own criteria for creation of log files. Note that this handler is called
    /// on EVERY log message 'write'; you may cache the log file name calculation in your handler to avoid any potential overhead in case of high-load logger usage.
    /// For example:
    /// </remarks>
    /// <example>
    /// FormatLogFileName = (fileName) => {
    ///   return string.Format(Path.GetFileNameWithoutExtension(fileName) + "_{0:dd}-{0:MM}-{0:yyyy}" + Path.GetExtension(fileName), DateTime.UtcNow); 
    /// };
    /// </example>
    public Func<string, string> FormatLogFileName => (fileName) => string.Format(System.Globalization.CultureInfo.InvariantCulture, Path.GetFileNameWithoutExtension(fileName) + "_{0:dd}-{0:MM}-{0:yyyy}" + Path.GetExtension(fileName), DateTime.UtcNow);

    /// <summary>
    /// Custom handler for log file errors
    /// </summary>
    /// <remarks>If this handler is provided file open exception (on <code>FileLoggerProvider</code> creation) will be suppressed.
    /// You can handle file error exception according to your app's logic, and propose an alternative log file name (if you want to keep file logger working).
    /// </remarks>
    /// <example>
    /// HandleFileError = (err) => {
    ///   err.UseNewLogFileName( Path.GetFileNameWithoutExtension(err.LogFileName)+ "_alt" + Path.GetExtension(err.LogFileName) );
    /// };
    /// </example>
    public Action<LogFileError>? HandleFileError { get; set; }

    /// <summary>
    /// Custom formatter for the log entry line. 
    /// </summary>
    public Func<LogMessage, string>? FormatLogEntry { get; set; }

    /// <summary>
    /// Custom filter for the log entry.
    /// </summary>
    public Func<LogMessage, bool>? FilterLogEntry { get; set; }

    /// <summary>
    /// Minimal logging level for the file logger.
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
        this.entryQueue.CompleteAdding();

        try
        {
            this.processQueueTask.Wait(1500);  // the same as in ConsoleLogger
        }
        catch (TaskCanceledException)
        {
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException) { }

        this.loggers.Clear();
        this.fileWriter.Close();

        GC.SuppressFinalize(this);
    }

    #endregion
}
