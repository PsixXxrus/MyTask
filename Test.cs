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
/// Типы логов — определяют, в какую папку писать.
/// </summary>
public enum LogType
{
    Init,
    Services,
    Database
}

/// <summary>
/// Конфигурация логгера.
/// </summary>
public class LoggerOptions
{
    /// <summary> Включить логирование Debug. </summary>
    public bool EnableDebug { get; set; } = true;

    /// <summary> Включить логирование Info. </summary>
    public bool EnableInfo { get; set; } = true;

    /// <summary> Включить логирование Warning. </summary>
    public bool EnableWarning { get; set; } = true;

    /// <summary> Включить логирование Error. </summary>
    public bool EnableError { get; set; } = true;

    /// <summary> Писать ли логи в JSON-формате. </summary>
    public bool EnableJson { get; set; } = false;

    /// <summary> Корневая папка для логов. </summary>
    public string BasePath { get; set; } = "logs";

    /// <summary> Количество дней, в течение которых хранятся логи. </summary>
    public int RetentionDays { get; set; } = 7;
}

/// <summary>
/// Асинхронный потокобезопасный логгер с поддержкой JSON, ротации, auto-caller-info.
/// </summary>
public static class Logger
{
    /// <summary> Очередь сообщений для асинхронной записи. </summary>
    private static readonly BlockingCollection<string> _queue = new();

    private static LoggerOptions _options;
    private static Task _writerTask;
    private static bool _running;
    private static bool _initialized;

    /// <summary>
    /// Инициализирует логгер. Вызывается один раз при старте приложения.
    /// </summary>
    public static void Init(LoggerOptions options)
    {
        if (_initialized)
            return;

        _initialized = true;
        _options = options;

        // Создание папок по типам логов
        foreach (var type in Enum.GetValues<LogType>())
        {
            string dir = Path.Combine(_options.BasePath, type.ToString().ToLower());
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        // Очистка старых файлов
        CleanupOldLogs();

        _running = true;

        // Фоновая задача для записи логов в файл
        _writerTask = Task.Run(ProcessQueue);

        Log(LogType.Init, LogLevel.Info, "Logger initialized");
    }

    /// <summary>
    /// Корректно останавливает логгер и дописывает все очереди.
    /// </summary>
    public static void Stop()
    {
        _running = false;
        _queue.CompleteAdding();
        _writerTask?.Wait();
    }

    /// <summary>
    /// Основной метод логирования.
    /// </summary>
    /// <param name="type">Тип лога (папка).</param>
    /// <param name="level">Уровень логирования.</param>
    /// <param name="message">Сообщение.</param>
    /// <param name="caller">Автоматически: имя метода.</param>
    /// <param name="filePath">Автоматически: путь к файлу-вызывателю.</param>
    public static void Log(
        LogType type,
        LogLevel level,
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        if (!_initialized)
            Init(new LoggerOptions()); // автоинициализация по умолчанию

        if (!IsEnabled(level))
            return;

        string className = Path.GetFileNameWithoutExtension(filePath);
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

        // Читабельный текстовый формат
        string textLine =
            $"[{time}] [{type}] [{level}] [{className}.{caller}] {message}";

        // Вывод в консоль
        WriteToConsole(level, textLine);

        // Выбор формата JSON/PLAIN
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
            : textLine;

        // Добавляем в очередь
        _queue.Add(FormatForFile(type, content));
    }

    /// <summary>
    /// Логирование исключений с автоматическим сбором StackTrace.
    /// </summary>
    public static void LogException(
        LogType type,
        Exception ex,
        string message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = "")
    {
        var sb = new StringBuilder();

        sb.AppendLine(message ?? "Exception occurred");
        sb.AppendLine(ex.Message);
        sb.AppendLine(ex.StackTrace);

        if (ex.InnerException != null)
        {
            sb.AppendLine("Inner Exception:");
            sb.AppendLine(ex.InnerException.Message);
            sb.AppendLine(ex.InnerException.StackTrace);
        }

        Log(type, LogLevel.Error, sb.ToString(), caller, filePath);
    }

    /// <summary>
    /// Проверка, включён ли уровень логов.
    /// </summary>
    private static bool IsEnabled(LogLevel level) =>
        level switch
        {
            LogLevel.Debug => _options.EnableDebug,
            LogLevel.Info => _options.EnableInfo,
            LogLevel.Warning => _options.EnableWarning,
            LogLevel.Error => _options.EnableError,
            _ => true
        };

    /// <summary>
    /// Формирует запись вида "путь||сообщение".
    /// </summary>
    private static string FormatForFile(LogType type, string line)
    {
        string folder = Path.Combine(_options.BasePath, type.ToString().ToLower());
        string file = Path.Combine(folder, $"{DateTime.Now:yyyy-MM-dd}.log");

        // Ежедневная ротация + удаление старых логов
        CleanupOldLogs();

        return file + "||" + line;
    }

    /// <summary>
    /// Фоновая асинхронная запись очереди в файлы.
    /// </summary>
    private static async Task ProcessQueue()
    {
        foreach (string entry in _queue.GetConsumingEnumerable())
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
                // Повторная попытка через паузу
                Thread.Sleep(10);
                await File.AppendAllTextAsync(filePath, text + Environment.NewLine);
            }
        }
    }

    /// <summary>
    /// Удаляет файлы логов старше RetentionDays.
    /// </summary>
    private static void CleanupOldLogs()
    {
        foreach (var type in Enum.GetValues<LogType>())
        {
            string folder = Path.Combine(_options.BasePath, type.ToString().ToLower());
            if (!Directory.Exists(folder))
                continue;

            var files = Directory.GetFiles(folder, "*.log");

            foreach (var file in files)
            {
                try
                {
                    var creation = File.GetCreationTime(file);

                    if ((DateTime.Now - creation).TotalDays > _options.RetentionDays)
                        File.Delete(file);
                }
                catch
                {
                    // Игнорируем ошибки удаления, логгер не должен падать
                }
            }
        }
    }

    /// <summary>
    /// Цветной вывод в консоль в зависимости от уровня логирования.
    /// </summary>
    private static void WriteToConsole(LogLevel level, string line)
    {
        ConsoleColor color = level switch
        {
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Debug => ConsoleColor.Cyan,
            _ => ConsoleColor.Gray
        };

        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(line);
        Console.ForegroundColor = previous;
    }
}