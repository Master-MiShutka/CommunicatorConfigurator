namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;

/// <summary>
/// Represents a file error context
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("PropertyChangedAnalyzers.PropertyChanged", "INPC001:The class has mutable properties and should implement INotifyPropertyChanged.", Justification = "<Pending>")]
public class LogFileError
{
    /// <summary>
    /// Exception that occurs on the file operation
    /// </summary>
    public Exception Error { get; private set; }

    /// <summary>
    /// Current log file name.
    /// </summary>
    public string LogFileName { get; private set; }

    internal LogFileError(string logFileName, Exception ex)
    {
        this.LogFileName = logFileName;
        this.Error = ex;
    }

    internal string? NewLogFileName { get; private set; }

    /// <summary>
    /// Suggests a new log file name to use instead of the current one
    /// </summary>
    /// <remarks>
    /// If proposed file name also leads to a file error this will break a file logger: errors are not handled recursively
    /// </remarks>
    /// <param name="newLogFileName">a new log file name</param>
    public void UseNewLogFileName(string newLogFileName)
    {
        this.NewLogFileName = newLogFileName;
    }
}
