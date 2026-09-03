using MTE.Core.Interfaces;

namespace MTE.Engine.Logging;

public sealed class FileMigrationLogger : IMigrationLogger
{
    private readonly string _logFile;
    private readonly object _lock = new();

    public FileMigrationLogger()
    {
        var logDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "MigrationToolkitEnterprise",
            "Logs");

        Directory.CreateDirectory(logDirectory);

        _logFile = Path.Combine(
            logDirectory,
            $"MTE_{DateTime.Now:yyyyMMdd}.log");
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARNING", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    public void Success(string message)
    {
        Write("SUCCESS", message);
    }

    private void Write(string level, string message)
    {
        lock (_lock)
        {
            var line =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            File.AppendAllText(
                _logFile,
                line + Environment.NewLine);
        }
    }
}