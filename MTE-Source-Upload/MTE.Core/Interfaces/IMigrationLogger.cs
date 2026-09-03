namespace MTE.Core.Interfaces;

public interface IMigrationLogger
{
    void Info(string message);

    void Warning(string message);

    void Error(string message);

    void Success(string message);
}