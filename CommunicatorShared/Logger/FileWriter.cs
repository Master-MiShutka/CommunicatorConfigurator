namespace TMP.Work.CommunicatorPSDTU.Common.Logger;

using System;
using System.IO;
using System.Linq;

internal class FileWriter : IDisposable
{
    readonly TextFileLoggerProvider textFileLogProvider;
    string logFileName;
    Stream? logFileStream;
    TextWriter? logFileTextWriter;

    // cache last returned base log file name to avoid excessive checks in CheckForNewLogFile.isBaseFileNameChanged
    string? lastBaseLogFileName;

    private readonly long fileSizeLimitBytes = 1_048_576;
    private readonly int maxRollingFiles = 5;
    private readonly bool rollingFilesConvention = true;

    internal FileWriter(TextFileLoggerProvider textFileLogProvider)
    {
        this.textFileLogProvider = textFileLogProvider;
        this.logFileName = this.textFileLogProvider.LogFileName;

        this.DetermineLastFileLogName();
        this.OpenFile(true);
    }

    private string GetBaseLogFileName()
    {
        var fileName = this.textFileLogProvider.LogFileName;
        if (this.textFileLogProvider.FormatLogFileName != null)
        {
            fileName = this.textFileLogProvider.FormatLogFileName(fileName);
        }

        return fileName;
    }

    #region Internal methods

    internal void UseNewLogFile(string newLogFileName)
    {
        this.textFileLogProvider.LogFileName = newLogFileName;
        this.DetermineLastFileLogName(); // preserve all existing logic related to 'FormatLogFileName' and rolling files
        this.CreateLogFileStream(true);  // if file error occurs here it is not handled by 'HandleFileError' recursively
    }

    internal void WriteMessage(string message, bool flush)
    {
        if (this.logFileTextWriter != null)
        {
            this.CheckForNewLogFile();
            this.logFileTextWriter.WriteLine(message);
            if (flush)
            {
                this.logFileTextWriter.Flush();
            }
        }
    }

    internal void Close()
    {
        this.logFileTextWriter?.Dispose();

        this.logFileStream?.Dispose();
    }

    #endregion

    #region Private methods


    #endregion

    private void DetermineLastFileLogName()
    {
        var baseLogFileName = this.GetBaseLogFileName();
        this.lastBaseLogFileName = baseLogFileName;
        if (this.fileSizeLimitBytes > 0)
        {
            // rolling file is used
            if (this.rollingFilesConvention)
            {
                var logFiles = this.GetExistingLogFiles(baseLogFileName);
                if (logFiles.Length > 0)
                {
                    var lastFileInfo = logFiles
                            .OrderByDescending(fInfo => fInfo.Name)
                            .OrderByDescending(fInfo => fInfo.LastWriteTime).First();
                    this.logFileName = lastFileInfo.FullName;
                }
                else
                {
                    // no files yet, use default name
                    this.logFileName = baseLogFileName;
                }
            }
            else
            {
                this.logFileName = baseLogFileName;
            }
        }
        else
        {
            this.logFileName = baseLogFileName;
        }
    }

    private void CreateLogFileStream(bool append)
    {
        var fileInfo = new FileInfo(this.logFileName);
        // Directory.Create will check if the directory already exists,
        // so there is no need for a "manual" check first.
        fileInfo.Directory?.Create();

        this.logFileStream = new FileStream(this.logFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        if (append)
        {
            this.logFileStream.Seek(0, SeekOrigin.End);
        }
        else
        {
            this.logFileStream.SetLength(0); // clear the file
        }
        this.logFileTextWriter = new StreamWriter(this.logFileStream);
    }

    private void OpenFile(bool append)
    {
        try
        {
            this.CreateLogFileStream(append);
        }
        catch (Exception ex)
        {
            if (this.textFileLogProvider.HandleFileError != null)
            {
                var fileErr = new LogFileError(this.logFileName, ex);
                this.textFileLogProvider.HandleFileError(fileErr);
                if (fileErr.NewLogFileName != null)
                {
                    this.UseNewLogFile(fileErr.NewLogFileName);
                }
            }
            else
            {
                throw; // do not handle by default to preserve backward compatibility
            }
        }
    }

    private string GetNextFileLogName()
    {
        var baseLogFileName = this.GetBaseLogFileName();

        // if file does not exist or file size limit is not reached - do not add rolling file index
        if (!System.IO.File.Exists(baseLogFileName) || this.fileSizeLimitBytes <= 0 || new System.IO.FileInfo(baseLogFileName).Length < this.fileSizeLimitBytes)
        {
            return baseLogFileName;
        }

        if (this.rollingFilesConvention)
        {
            int currentFileIndex = this.GetIndexFromFile(baseLogFileName, this.logFileName);
            var nextFileIndex = currentFileIndex + 1;
            if (this.maxRollingFiles > 0)
            {
                nextFileIndex %= this.maxRollingFiles;
            }
            return this.GetFileFromIndex(baseLogFileName, nextFileIndex);
        }
        else
        {
            var logFiles = this.GetExistingLogFiles(baseLogFileName);
            if (logFiles.Length > 0)
            {
                foreach (var finfo in logFiles.OrderByDescending(fInfo => fInfo.Name))
                {
                    var index = this.GetIndexFromFile(baseLogFileName, finfo.Name);
                    if (this.maxRollingFiles > 0 && index >= this.maxRollingFiles - 1)
                    {
                        continue;
                    }
                    var moveFile = this.GetFileFromIndex(baseLogFileName, index + 1);
                    if (System.IO.File.Exists(moveFile))
                    {
                        System.IO.File.Delete(moveFile);
                    }
                    System.IO.File.Move(finfo.FullName, moveFile);
                }
            }
            return baseLogFileName;
        }
    }

    private void CheckForNewLogFile()
    {
        bool openNewFile = false;
        if (isMaxFileSizeThresholdReached() || isBaseFileNameChanged())
        {
            openNewFile = true;
        }

        if (openNewFile)
        {
            this.Close();
            this.logFileName = this.GetNextFileLogName();
            this.OpenFile(false);
        }

        bool isMaxFileSizeThresholdReached() => this.fileSizeLimitBytes > 0 && this.logFileStream?.Length > this.fileSizeLimitBytes;

        bool isBaseFileNameChanged()
        {
            if (this.textFileLogProvider.FormatLogFileName != null)
            {
                var baseLogFileName = this.GetBaseLogFileName();
                if (baseLogFileName != this.lastBaseLogFileName)
                {
                    this.lastBaseLogFileName = baseLogFileName;
                    return true;
                }
                return false;
            }
            return false;
        }
    }

    /// <summary>
    /// Returns the index of a file or 0 if none found
    /// </summary>
    private int GetIndexFromFile(string baseLogFileName, string filename)
    {
        var baseFileNameOnly = Path.GetFileNameWithoutExtension(baseLogFileName.AsSpan());
        var currentFileNameOnly = Path.GetFileNameWithoutExtension(filename.AsSpan());

        var suffix = currentFileNameOnly[baseFileNameOnly.Length..];
        if (suffix.Length > 0 && int.TryParse(suffix, out var parsedIndex))
        {
            return parsedIndex;
        }
        return 0;
    }

    private string GetFileFromIndex(string baseLogFileName, int index)
    {
        // Contact for ReadOnlySpan<char> is not available in both netstandard2.0 and netstandard2.1
        var nextFileName = string.Concat(Path.GetFileNameWithoutExtension(baseLogFileName.AsSpan()), index > 0 ? index.ToString(System.Globalization.CultureInfo.InvariantCulture) : "", Path.GetExtension(baseLogFileName.AsSpan()));
        return string.Concat(Path.Join(Path.GetDirectoryName(baseLogFileName.AsSpan()), nextFileName.AsSpan()));
    }

    private FileInfo[] GetExistingLogFiles(string baseLogFileName)
    {
        var logFileMask = Path.GetFileNameWithoutExtension(baseLogFileName) + "*" + Path.GetExtension(baseLogFileName);
        var logDirName = Path.GetDirectoryName(baseLogFileName);
        if (string.IsNullOrEmpty(logDirName))
        {
            logDirName = Directory.GetCurrentDirectory();
        }

        var logdir = new DirectoryInfo(logDirName);
        return logdir.Exists ? logdir.GetFiles(logFileMask, SearchOption.TopDirectoryOnly) : [];
    }

    #region IDisposable implementation

    public void Dispose()
    {
        this.logFileTextWriter?.Flush();
        this.logFileStream?.Flush();

        this.logFileTextWriter?.Close();
        this.logFileStream?.Close();

        this.logFileTextWriter?.Dispose();
        this.logFileStream?.Dispose();
    }

    #endregion
}
