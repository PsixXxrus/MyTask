using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Уровни логирования.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// Типы логов — определяют папку для записи.
/// </summary>
public enum LogType
{
    Init,
    Services,
    Database
}

/// <summary>
/// Настройки логгера.
/// </summary>
public class LoggerOptions
{
    public bool EnableDebug { get; set; } = true;
    public bool EnableInfo { get; set; } = true;
    public bool EnableWarning { get; set; } = true;
    public bool EnableError { get; set; } = true;

    public bool EnableJson { get; set; } = false;

    public string BasePath { get; set; } = "logs";

    public int RetentionDays { get; set; } = 7;
}

/// <summary>
/// Асинхронный потокобезопасный логгер с LogType.
/// </summary>
public static class Logger
{
    private static readonly BlockingCollection<string> _queue = new();

    private static LoggerOptions _options;
    private static Task _writerTask;
    private static bool _running;
    private static bool _initialized;

    /// <summary>
    /// Инициализация логгера (однократная).
    /// </summary>
    public static void Init(LoggerOptions options)
    {
        if (_initialized)
            return;

        _initialized = true;
        _options = options;

        foreach (var type in Enum.GetValues<LogType>())
        {
            string dir = Path.Combine(_options.BasePath, type.ToString().ToLower());
            Directory.CreateDirectory(dir);
        }

        CleanupOldLogs();

        _running = true;
        _writerTask = Task.Run(ProcessQueue);

        Info(LogType.Init, "Logger initialized");
    }

    /// <summary>
    /// Основной метод логирования.
    /// </summary>
    public static void Log(
        LogType type,
        LogLevel level,
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        if (!_initialized)
            Init(new LoggerOptions());

        if (!IsEnabled(level))
            return;

        string className = Path.GetFileNameWithoutExtension(filePath);
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        string formatted =
            $"[{time}] [{type}] [{level}] [{className}.{caller}] {message}";

        WriteToConsole(level, formatted);

        string content = _options.EnableJson
            ? JsonSerializer.Serialize(new
            {
                time,
                type = type.ToString(),
                level = level.ToString(),
                className,
                method = caller,
                message
            })
            : formatted;

        _queue.Add(FormatForFile(type, content));
    }

    /// <summary>
    /// Упрощённое логирование исключений.
    /// </summary>
    public static void Exception(
        LogType type,
        Exception ex,
        string text = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        var sb = new StringBuilder();
        if (text != null)
            sb.AppendLine(text);

        sb.AppendLine(ex.Message);
        sb.AppendLine(ex.StackTrace);

        if (ex.InnerException != null)
        {
            sb.AppendLine("Inner:");
            sb.AppendLine(ex.InnerException.Message);
            sb.AppendLine(ex.InnerException.StackTrace);
        }

        Log(type, LogLevel.Error, sb.ToString(), caller, filePath);
    }

    // ───────────────────────────────────────────────────────────────
    // Удобные короткие методы
    // ───────────────────────────────────────────────────────────────

    public static void Info(LogType type, string msg,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "") =>
        Log(type, LogLevel.Info, msg, caller, filePath);

    public static void Warning(LogType type, string msg,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "") =>
        Log(type, LogLevel.Warning, msg, caller, filePath);

    public static void Error(LogType type, string msg,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "") =>
        Log(type, LogLevel.Error, msg, caller, filePath);

    public static void Debug(LogType type, string msg,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "") =>
        Log(type, LogLevel.Debug, msg, caller, filePath);

    // ───────────────────────────────────────────────────────────────

    private static bool IsEnabled(LogLevel level) =>
        level switch
        {
            LogLevel.Debug => _options.EnableDebug,
            LogLevel.Info => _options.EnableInfo,
            LogLevel.Warning => _options.EnableWarning,
            LogLevel.Error => _options.EnableError,
            _ => true
        };

    private static string FormatForFile(LogType type, string line)
    {
        string folder = Path.Combine(_options.BasePath, type.ToString().ToLower());
        string file = Path.Combine(folder, $"{DateTime.Now:yyyy-MM-dd}.log");

        CleanupOldLogs();

        return file + "||" + line;
    }

    private static async Task ProcessQueue()
    {
        foreach (var entry in _queue.GetConsumingEnumerable())
        {
            if (!_running && _queue.Count == 0)
                break;

            var parts = entry.Split("||", 2);
            string filePath = parts[0];
            string text = parts[1];

            try
            {
                await File.AppendAllTextAsync(filePath, text + Environment.NewLine);
            }
            catch
            {
                Thread.Sleep(20);
                await File.AppendAllTextAsync(filePath, text + Environment.NewLine);
            }
        }
    }

    /// <summary>Удаляет логи старше RetentionDays.</summary>
    private static void CleanupOldLogs()
    {
        foreach (var type in Enum.GetValues<LogType>())
        {
            string folder = Path.Combine(_options.BasePath, type.ToString().ToLower());
            if (!Directory.Exists(folder))
                continue;

            foreach (string file in Directory.GetFiles(folder, "*.log"))
            {
                try
                {
                    var creation = File.GetCreationTime(file);
                    if ((DateTime.Now - creation).TotalDays > _options.RetentionDays)
                        File.Delete(file);
                }
                catch { }
            }
        }
    }

    private static void WriteToConsole(LogLevel level, string text)
    {
        ConsoleColor color = level switch
        {
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Debug => ConsoleColor.Cyan,
            _ => ConsoleColor.Gray
        };

        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = prev;
    }
}