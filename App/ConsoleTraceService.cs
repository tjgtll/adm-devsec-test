using Yuniql.Extensibility;

namespace App;

public class ConsoleTraceService : ITraceService
{
    public bool IsDebugEnabled { get; set; } = false;
    public bool IsTraceSensitiveData { get; set; } = false;
    public bool IsTraceToDirectory { get; set; } = false;
    public bool IsTraceToFile { get; set; } = false;
    public string TraceDirectory { get; set; } = string.Empty;

    public void Info(string message, object? payload = null)
    {
        Console.Write($"INF   {DateTime.UtcNow:o}   {message}{Environment.NewLine}");
    }

    public void Error(string message, object? payload = null)
    {
        Console.Write($"ERR   {DateTime.UtcNow:o}   {message}{Environment.NewLine}");
    }

    public void Debug(string message, object? payload = null)
    {
        if (IsDebugEnabled)
        {
            Console.Write($"DBG   {DateTime.UtcNow:o}   {message}{Environment.NewLine}");
        }
    }

    public void Success(string message, object? payload = null)
    {
        Info(message, payload);
    }

    public void Warn(string message, object? payload = null)
    {
        Console.Write($"WRN   {DateTime.UtcNow:o}   {message}{Environment.NewLine}");
    }
}