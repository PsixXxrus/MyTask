Супер! Ниже — полная пошаговая инструкция по созданию и установке Worker Service (.NET 8), который каждые 6 часов копирует только новые/изменённые файлы по нескольким заданиям. Везде добавил комментарии в коде.

⸻

1) Создаём проект

dotnet new worker -n CopyService
cd CopyService

Шаблон worker уже готов к запуску как служба Windows: останется включить .UseWindowsService() и добавить нашу логику.

⸻

2) Структура файлов

Добавим 3 файла и поправим 2 существующих:

CopyService/
  Program.cs              ← правим
  Worker.cs               ← правим
  appsettings.json        ← добавим (настройки)
  Models.cs               ← добавим (POCO настройки/задания)
  CopyEngine.cs           ← добавим (логика копирования)


⸻

3) appsettings.json — все настройки в одном месте

{
  "Schedule": {
    "IntervalHours": 6,           // интервал между циклами, часы
    "RunAtStartup": true          // запускать цикл сразу при старте службы
  },
  "CopyOptions": {
    "MaxDegreeOfParallelism": 8,  // параллельных копий (подбери под диск/сеть)
    "RetryCount": 3,              // ретраев при ошибке I/O
    "RetryDelaySeconds": 5,       // пауза между ретраями
    "PreserveTimestamps": true,   // сохранять LastWriteTime
    "Mirror": false,              // ГЛОБАЛЬНО: удалять лишние на приёмнике (осторожно!)
    "TimeSkewSeconds": 2,         // допуск по времени модификации (NAS/разные ФС)
    "ExcludeFiles": [ "*.tmp", "*.log" ], // глобальные исключения файлов
    "ExcludeDirs":  [ "Temp", "Cache" ]   // глобальные исключения папок
  },
  "LogsDir": "Logs",              // куда писать логи (относительно EXE, можно абсолютный)
  "Jobs": [
    {
      "Name": "Job1",
      "Source": "\\\\ServerA\\Share1",
      "Destination": "\\\\ServerB\\Share1"
      // "Mirror": true,                     // можно переопределить Mirror на уровне задания
      // "ExcludeFiles": [ "*.bak" ],        // можно переопределить исключения
      // "ExcludeDirs":  [ "node_modules" ]
    },
    {
      "Name": "Job2",
      "Source": "\\\\ServerA\\Share2",
      "Destination": "\\\\ServerB\\Share2"
    }
  ]
}


⸻

4) Models.cs — модели конфигурации

namespace CopyService;

public sealed class ScheduleOptions
{
    public int  IntervalHours  { get; init; } = 6;
    public bool RunAtStartup   { get; init; } = true;
}

public sealed class CopyOptions
{
    public int  MaxDegreeOfParallelism { get; init; } = 8;
    public int  RetryCount             { get; init; } = 3;
    public int  RetryDelaySeconds      { get; init; } = 5;
    public bool PreserveTimestamps     { get; init; } = true;
    public bool Mirror                 { get; init; } = false; // глобально
    public int  TimeSkewSeconds        { get; init; } = 2;     // допуск на «дрейф» времени

    public string[] ExcludeFiles { get; init; } = Array.Empty<string>();
    public string[] ExcludeDirs  { get; init; } = Array.Empty<string>();
}

public sealed class CopyJob
{
    public string Name        { get; init; } = "Job";
    public string Source      { get; init; } = "";   // UNC или локальный путь
    public string Destination { get; init; } = "";

    // Переопределения для конкретного задания (необязательно)
    public bool?      Mirror       { get; init; } = null;
    public string[]?  ExcludeFiles { get; init; } = null;
    public string[]?  ExcludeDirs  { get; init; } = null;
}

public sealed class AppSettings
{
    public ScheduleOptions Schedule    { get; init; } = new();
    public CopyOptions     CopyOptions { get; init; } = new();
    public List<CopyJob>   Jobs        { get; init; } = new();
    public string          LogsDir     { get; init; } = "Logs";
}


⸻

5) CopyEngine.cs — аккуратное инкрементальное копирование

using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CopyService;

public sealed class CopyEngine
{
    private readonly ILogger<CopyEngine> _log;

    public CopyEngine(ILogger<CopyEngine> log) => _log = log;

    /// Выполнить одно задание копирования
    public async Task RunJobAsync(
        CopyJob job,
        CopyOptions globalOpt,
        string logsDir,
        CancellationToken ct)
    {
        // Эффективные настройки для задания: локальные > глобальные
        var mirror      = job.Mirror ?? globalOpt.Mirror;
        var exFiles     = job.ExcludeFiles?.Length > 0 ? job.ExcludeFiles! : globalOpt.ExcludeFiles;
        var exDirs      = job.ExcludeDirs?.Length  > 0 ? job.ExcludeDirs!  : globalOpt.ExcludeDirs;

        // Подготовим Regex для исключений
        var exFileRx = exFiles.Select(GlobToRegex).ToArray();
        var exDirRx  = exDirs.Select(GlobToRegex).ToArray();

        // Папка логов (создастся при необходимости)
        var logsRoot = Path.IsPathRooted(logsDir)
            ? logsDir
            : Path.Combine(AppContext.BaseDirectory, logsDir);
        Directory.CreateDirectory(logsRoot);

        var logFile = Path.Combine(logsRoot, $"{San(job.Name)}_{DateTime.UtcNow:yyyy-MM-dd}.log");
        Append(logFile, $"=== [{job.Name}] {job.Source} -> {job.Destination} ===");

        // Соберём список файлов источника (с обходом недоступных папок)
        var files = EnumerateFilesSafe(job.Source, exDirRx).ToList();
        Append(logFile, $"[{job.Name}] source files: {files.Count}");

        var errors = new ConcurrentBag<string>();

        // Параллельная обработка, но с ограничением DegreeOfParallelism
        await Parallel.ForEachAsync(files, new ParallelOptions
        {
            MaxDegreeOfParallelism = globalOpt.MaxDegreeOfParallelism,
            CancellationToken = ct
        }, async (srcPath, token) =>
        {
            // Прерывание по требованию
            token.ThrowIfCancellationRequested();

            // Файлы по маскам исключений пропускаем
            if (exFileRx.Any(rx => rx.IsMatch(Path.GetFileName(srcPath)))) return;

            var rel    = Path.GetRelativePath(job.Source, srcPath);
            var dstPath = Path.Combine(job.Destination, rel);
            var dstDir  = Path.GetDirectoryName(dstPath)!;

            try
            {
                Directory.CreateDirectory(dstDir);

                if (!NeedCopy(srcPath, dstPath, globalOpt.TimeSkewSeconds))
                    return; // уже существует и не новее/не отличается по размеру

                // Ретраи копирования на случай временных ошибок
                for (int attempt = 1; attempt <= globalOpt.RetryCount; attempt++)
                {
                    try
                    {
                        // Открываем источник в режиме Read + Share.Read (не блокируем других читателей)
                        using var src = new FileStream(
                            srcPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                            bufferSize: 1024 * 1024, options: FileOptions.SequentialScan);

                        // Создаём/переписываем целевой файл
                        using var dst = new FileStream(
                            dstPath, FileMode.Create, FileAccess.Write, FileShare.None,
                            bufferSize: 1024 * 1024, options: FileOptions.SequentialScan);

                        await src.CopyToAsync(dst, 1024 * 1024, token); // буфер 1МБ

                        if (globalOpt.PreserveTimestamps)
                        {
                            var t = File.GetLastWriteTimeUtc(srcPath);
                            File.SetLastWriteTimeUtc(dstPath, t);
                        }

                        Append(logFile, $"[{job.Name}] Copied: {rel}");
                        break; // успех
                    }
                    catch (IOException ex) when (attempt < globalOpt.RetryCount)
                    {
                        // временный сбой: подождём и попробуем снова
                        await Task.Delay(TimeSpan.FromSeconds(globalOpt.RetryDelaySeconds), token);
                        Append(logFile, $"[{job.Name}] Retry {attempt}/{globalOpt.RetryCount} {rel}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"[{job.Name}] FAILED {rel}: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"[{job.Name}] ERROR {rel}: {ex.Message}");
            }
        });

        // Зеркалирование (опционально): удаляем файлы, которых нет в источнике
        if (mirror)
            MirrorDelete(job.Source, job.Destination, exDirRx, exFileRx, ct, logFile, job.Name);

        // Выведем накопившиеся ошибки в лог
        foreach (var e in errors)
            Append(logFile, e);

        Append(logFile, $"=== [{job.Name}] done ===");
    }

    /// Решение копировать: если файла нет, отличается размер, или источник «заметно» новее
    private static bool NeedCopy(string srcPath, string dstPath, int skewSeconds)
    {
        if (!File.Exists(dstPath)) return true;

        var s = new FileInfo(srcPath);
        var d = new FileInfo(dstPath);

        if (s.Length != d.Length) return true;

        // Допуск по времени (например, NAS округляет метки до 2 сек)
        var skew = TimeSpan.FromSeconds(Math.Max(0, skewSeconds));
        return s.LastWriteTimeUtc - d.LastWriteTimeUtc > skew;
    }

    /// Безопасный обход дерева, игнорируя недоступные папки и исключая каталоги по маскам
    private static IEnumerable<string> EnumerateFilesSafe(string root, Regex[] excludeDirs)
    {
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var dir = stack.Pop();

            IEnumerable<string> subdirs = Array.Empty<string>();
            IEnumerable<string> files   = Array.Empty<string>();

            try
            {
                subdirs = Directory.EnumerateDirectories(dir);
                files   = Directory.EnumerateFiles(dir);
            }
            catch
            {
                // пропускаем недоступные каталоги
            }

            foreach (var f in files)
                yield return f;

            foreach (var d in subdirs)
            {
                var name = Path.GetFileName(d);
                if (excludeDirs.Any(rx => rx.IsMatch(name))) continue;
                stack.Push(d);
            }
        }
    }

    /// Зеркалирование: удаляем лишние файлы/пустые папки на приёмнике
    private static void MirrorDelete(
        string srcRoot, string dstRoot,
        Regex[] exDirs, Regex[] exFiles,
        CancellationToken ct, string logFile, string jobName)
    {
        foreach (var dstFile in EnumerateFilesSafe(dstRoot, exDirs))
        {
            ct.ThrowIfCancellationRequested();

            var rel    = Path.GetRelativePath(dstRoot, dstFile);
            var srcFile = Path.Combine(srcRoot, rel);

            if (exFiles.Any(rx => rx.IsMatch(Path.GetFileName(dstFile)))) continue;

            if (!File.Exists(srcFile))
            {
                try { File.Delete(dstFile); Append(logFile, $"[{jobName}] Deleted: {rel}"); }
                catch (Exception ex) { Append(logFile, $"[{jobName}] Delete failed {rel}: {ex.Message}"); }
            }
        }

        // Чистим пустые каталоги снизу вверх
        foreach (var d in Directory.EnumerateDirectories(dstRoot, "*", SearchOption.AllDirectories)
                                   .OrderByDescending(s => s.Length))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(d).Any())
                    Directory.Delete(d);
            }
            catch { /* игнор */ }
        }
    }

    // Глоб → Regex (*.tmp → ^.*\.tmp$)
    private static Regex GlobToRegex(string glob)
    {
        var pattern = "^" + Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    }

    // Быстрая запись строки в лог (UTC-время + сообщение)
    private static void Append(string path, string line)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.AppendAllText(path, $"{DateTime.UtcNow:O} {line}{Environment.NewLine}", Encoding.UTF8);
    }

    // Имя файла-логов без «запрещённых» символов
    private static string San(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}


⸻

6) Worker.cs — цикл раз в 6 часов, без пересечений

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopyService;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly CopyEngine _engine;
    private readonly AppSettings _cfg;

    public Worker(ILogger<Worker> logger, CopyEngine engine, IOptions<AppSettings> cfg)
    {
        _logger = logger;
        _engine = engine;
        _cfg    = cfg.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Max(1, _cfg.Schedule.IntervalHours));
        _logger.LogInformation("CopyService started. Interval: {Interval}, Jobs: {Count}",
            interval, _cfg.Jobs.Count);

        // Первый запуск — сразу (если включено), затем каждые interval
        var firstRun = _cfg.Schedule.RunAtStartup;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (firstRun)
            {
                firstRun = false;
            }
            else
            {
                await Task.Delay(interval, stoppingToken);
                if (stoppingToken.IsCancellationRequested) break;
            }

            var started = DateTimeOffset.Now;

            try
            {
                foreach (var job in _cfg.Jobs)
                {
                    _logger.LogInformation(">>> [{Job}] {Src} -> {Dst}", job.Name, job.Source, job.Destination);
                    await _engine.RunJobAsync(job, _cfg.CopyOptions, _cfg.LogsDir, stoppingToken);
                }

                _logger.LogInformation("All jobs finished in {Elapsed}", DateTimeOffset.Now - started);
            }
            catch (OperationCanceledException)
            {
                // корректная остановка
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Copy cycle failed");
            }
        }
    }
}


⸻

7) Program.cs — включаем службу и DI

using CopyService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Host.CreateDefaultBuilder(args)
    .UseWindowsService() // превращаем Worker в службу Windows
    .ConfigureServices((ctx, services) =>
    {
        // Привязываем все секции из appsettings.json к AppSettings
        services.Configure<AppSettings>(ctx.Configuration);

        // Наш движок и фоновая служба
        services.AddSingleton<CopyEngine>();
        services.AddHostedService<Worker>();

        // Логи: по умолчанию → Event Viewer (в режиме службы) + консоль (в отладке)
        services.AddLogging(lb => lb.AddSimpleConsole());
    })
    .Build()
    .Run();


⸻

8) Сборка и проверка в консоли (удобно для отладки)

dotnet run

В консоли увидишь логи запуска. (При запуске из IDE/консоли, .UseWindowsService() не мешает.)

⸻

9) Публикация и установка как служба Windows

Публикация

dotnet publish -c Release -r win-x64 --self-contained false

Путь публикации (пример):
.\bin\Release\net8.0\win-x64\publish\

Скопируй туда appsettings.json (и поправь пути Source/Destination под боевые).

Установка службы (из-под Администратора)

sc.exe create CopyService binPath= "C:\Path\to\publish\CopyService.exe" start= auto
sc.exe description CopyService "Incremental file copy every 6 hours"
sc.exe start CopyService

Важно: служба должна запускаться под учёткой, имеющей доступ к сетевым шарам.
После установки открой services.msc → CopyService → вкладка Log On → укажи доменную учётку.
(Либо создай с sc.exe и затем поменяй Log On через GUI.)

Автоматический перезапуск при сбое (рекомендуется)

sc.exe failure CopyService reset= 0 actions= restart/60000


⸻

10) Где смотреть логи и что проверять
	•	Файлы логов: папка Logs (или та, что указана в LogsDir) рядом с EXE.
Формат: JobName_YYYY-MM-DD.log (UTC-времена).
	•	Журнал событий Windows: Event Viewer → Windows Logs → Application (логи Microsoft.Extensions.Logging).
	•	Права доступа: убедись, что учётка службы видит \\ServerA\... и \\ServerB\....
	•	Перфоманс: при необходимости уменьшай MaxDegreeOfParallelism, добавляй исключения, включай/выключай Mirror.

⸻

Полезные заметки
	•	Инкрементальность: копируем, только если файла нет, размер отличается или источник новее на величину больше TimeSkewSeconds (защита от округления времени на NAS).
	•	Исключения: глобальные маски из CopyOptions можно переопределить на уровне конкретного Job.
	•	Mirror (осторожно!): если включить, на приёмнике удалятся файлы, отсутствующие в источнике (с учётом исключений).
	•	Блокировки: если источник в момент копирования занят, сработают ретраи. При длительных блокировках задание продолжит со следующего файла.
	•	Нагрузка: задания выполняются последовательно, чтобы не «забивать» сеть/диск суммарно. Параллелизм — внутри одного задания.

⸻

Если дашь свои пары Source → Destination и пожелания (нужен ли Mirror, какие исключения, сколько потоков) — соберу тебе готовый appsettings.json именно под твою инфраструктуру.