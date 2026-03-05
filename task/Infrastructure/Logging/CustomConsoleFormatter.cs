using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace TestTask.Infrastructure.Logging;

public class CustomConsoleFormatter : ConsoleFormatter
{
    public CustomConsoleFormatter() : base("custom") { }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (logEntry.Exception == null && message == null)
        {
            return;
        }

        var logLevel = logEntry.LogLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            _ => logEntry.LogLevel.ToString().ToUpper()
        };

        var time = DateTime.Now.ToString("HH:mm:ss");
        
        // Format: {Time} {Level}: {Message}
        // Excludes category name (logEntry.Category)
        textWriter.Write($"{time} {logLevel}: {message}");
        
        if (logEntry.Exception != null)
        {
            textWriter.WriteLine();
            textWriter.Write(logEntry.Exception.ToString());
        }
        
        textWriter.WriteLine();
    }
}
