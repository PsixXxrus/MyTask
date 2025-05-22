using Cronos;

public enum TaskExecutionStatus
{
    Pending,
    Running,
    Success,
    Error
}

public class ScheduledTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name => Task?.Name ?? "Unnamed";
    public IBackgroundTask? Task { get; set; }

    // Настройки запуска
    public TimeSpan? Interval { get; set; } = null;
    public string? CronExpression { get; set; } = null;

    // Состояние
    public bool Enabled { get; set; } = true;
    public bool IsRunning { get; set; } = false;
    public bool RunOnStartup { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunTime { get; set; }
    public DateTime? NextRunTime { get; set; }

    // Диагностика
    public TaskExecutionStatus LastStatus { get; set; } = TaskExecutionStatus.Pending;
    public string? LastError { get; set; }

    public void CalculateNextRun()
    {
        if (CronExpression != null)
        {
            var cron = CronExpression.Parse(CronExpression, CronFormat.Standard);
            NextRunTime = cron.GetNextOccurrence(DateTime.UtcNow, TimeZoneInfo.Utc);
        }
        else if (Interval.HasValue)
        {
            NextRunTime = DateTime.UtcNow.Add(Interval.Value);
        }
        else
        {
            NextRunTime = null; // Одноразовая задача
        }
    }
}


public interface IBackgroundTask
{
    string Name { get; }
    Task ExecuteAsync(CancellationToken cancellationToken);
}


using System.Collections.Concurrent;

public class ScheduledTaskManager
{
    private readonly ConcurrentDictionary<string, ScheduledTask> _tasks = new();

    public IEnumerable<ScheduledTask> GetAllTasks() => _tasks.Values;

    public void AddTask(ScheduledTask task)
    {
        task.CalculateNextRun();
        _tasks[task.Id] = task;
    }

    public void RemoveTask(string id) => _tasks.TryRemove(id, out _);

    public void PauseTask(string id)
    {
        if (_tasks.TryGetValue(id, out var task))
            task.Enabled = false;
    }

    public void ResumeTask(string id)
    {
        if (_tasks.TryGetValue(id, out var task))
        {
            task.Enabled = true;
            task.CalculateNextRun();
        }
    }

    public ScheduledTask? GetNextExecutableTask()
    {
        return _tasks.Values
            .Where(t => t.Enabled && t.NextRunTime.HasValue && DateTime.UtcNow >= t.NextRunTime.Value && !t.IsRunning)
            .OrderBy(t => t.NextRunTime)
            .FirstOrDefault();
    }
}


using Microsoft.Extensions.Hosting;

public class ScheduledTaskService : BackgroundService
{
    private readonly ScheduledTaskManager _taskManager;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(1);

    public ScheduledTaskService(ScheduledTaskManager manager, IServiceProvider sp)
    {
        _taskManager = manager;
        _serviceProvider = sp;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Выполнение задач с флагом RunOnStartup
        foreach (var task in _taskManager.GetAllTasks().Where(t => t.RunOnStartup && t.Enabled))
        {
            _ = RunTaskAsync(task, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var task = _taskManager.GetNextExecutableTask();
            if (task != null)
            {
                _ = RunTaskAsync(task, stoppingToken);
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task RunTaskAsync(ScheduledTask task, CancellationToken token)
    {
        task.IsRunning = true;
        task.LastRunTime = DateTime.UtcNow;
        task.LastStatus = TaskExecutionStatus.Running;
        task.LastError = null;

        try
        {
            await task.Task!.ExecuteAsync(token);
            task.LastStatus = TaskExecutionStatus.Success;
        }
        catch (Exception ex)
        {
            task.LastStatus = TaskExecutionStatus.Error;
            task.LastError = ex.Message;
        }
        finally
        {
            task.IsRunning = false;
            task.CalculateNextRun();
        }
    }
}


public class ExampleTask : IBackgroundTask
{
    public string Name => "PrintTime";

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{DateTime.Now}] Задача выполнена.");
        return Task.CompletedTask;
    }
}




builder.Services.AddSingleton<ScheduledTaskManager>();
builder.Services.AddHostedService<ScheduledTaskService>();

// Пример добавления задачи при старте
var manager = new ScheduledTaskManager();

var cronTask = new ScheduledTask
{
    Task = new ExampleTask(),
    CronExpression = "*/2 * * * *", // Каждые 2 минуты
    RunOnStartup = true,
    Enabled = true
};

manager.AddTask(cronTask);

builder.Services.AddSingleton(manager);




@page "/taskscheduler"
@inject ScheduledTaskManager TaskManager

<h3>Фоновые задачи</h3>

<!-- Таблица для отображения всех задач -->
<table class="table table-striped">
    <thead>
        <tr>
            <th>Имя</th>
            <th>Статус</th>
            <th>След. запуск</th>
            <th>Последний запуск</th>
            <th>Последняя ошибка</th>
            <th>Управление</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var task in Tasks)
        {
            <tr>
                <td>@task.Name</td>
                <td>
                    @if (!task.Enabled)
                    {
                        <span class="badge bg-secondary">Пауза</span>
                    }
                    else if (task.IsRunning)
                    {
                        <span class="badge bg-warning text-dark">Выполняется</span>
                    }
                    else if (task.LastStatus == TaskExecutionStatus.Success)
                    {
                        <span class="badge bg-success">Успешно</span>
                    }
                    else if (task.LastStatus == TaskExecutionStatus.Error)
                    {
                        <span class="badge bg-danger">Ошибка</span>
                    }
                    else
                    {
                        <span class="badge bg-light text-dark">Ожидание</span>
                    }
                </td>
                <td>@(task.NextRunTime?.ToLocalTime().ToString("HH:mm:ss") ?? "-")</td>
                <td>@(task.LastRunTime?.ToLocalTime().ToString("HH:mm:ss") ?? "-")</td>
                <td>@(task.LastError ?? "-")</td>
                <td>
                    @if (task.Enabled)
                    {
                        <button class="btn btn-sm btn-warning me-1" @onclick="() => PauseTask(task.Id)">⏸ Пауза</button>
                    }
                    else
                    {
                        <button class="btn btn-sm btn-success me-1" @onclick="() => ResumeTask(task.Id)">▶ Продолжить</button>
                    }
                    <button class="btn btn-sm btn-danger" @onclick="() => DeleteTask(task.Id)">🗑 Удалить</button>
                </td>
            </tr>
        }
    </tbody>
</table>



@code {
    private List<ScheduledTask> Tasks = new();

    protected override void OnInitialized()
    {
        LoadTasks();  // Загружаем задачи при инициализации компонента
    }

    private void LoadTasks()
    {
        Tasks = TaskManager.GetAllTasks().ToList();  // Получаем все текущие задачи из менеджера
    }

    private void PauseTask(string id)
    {
        TaskManager.PauseTask(id);  // Приостанавливаем задачу
        LoadTasks();                // Обновляем список задач
    }

    private void ResumeTask(string id)
    {
        TaskManager.ResumeTask(id); // Возобновляем задачу
        LoadTasks();                // Обновляем список задач
    }

    private void DeleteTask(string id)
    {
        TaskManager.RemoveTask(id); // Удаляем задачу из списка
        LoadTasks();                // Обновляем UI
    }
}
