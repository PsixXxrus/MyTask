public class CustomsServiceController
{
    private readonly Dictionary<string, SemaphoreSlim> _taskLocks = new();

    private readonly object _lock = new();

    public async Task<bool> TryRunCustomTaskAsync(string taskName, Func<Task> task)
    {
        var semaphore = GetOrCreateSemaphore(taskName);

        if (!await semaphore.WaitAsync(0))
        {
            // Задача с таким именем уже выполняется
            return false;
        }

        try
        {
            await task();
            return true;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public bool IsTaskRunning(string taskName)
    {
        var semaphore = GetOrCreateSemaphore(taskName);
        return semaphore.CurrentCount == 0;
    }

    private SemaphoreSlim GetOrCreateSemaphore(string taskName)
    {
        lock (_lock)
        {
            if (!_taskLocks.TryGetValue(taskName, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _taskLocks[taskName] = semaphore;
            }

            return semaphore;
        }
    }
}



<button @onclick="RunUpdateFiles">Обновить файлы</button>
<button @onclick="RunScanDb">Сканировать БД</button>

@code {
    [Inject] CustomsService CustomsService { get; set; }

    private async Task RunUpdateFiles()
    {
        bool success = await CustomsService.RunManualTaskAsync("UpdateFileList");

        Console.WriteLine(success ? "Файлы обновляются" : "Уже выполняется");
    }

    private async Task RunScanDb()
    {
        bool success = await CustomsService.RunManualTaskAsync("ScanDatabase");

        Console.WriteLine(success ? "БД сканируется" : "Уже выполняется");
    }
}



public class CustomsService
{
    private readonly CustomsServiceController _controller;

    public CustomsService(CustomsServiceController controller)
    {
        _controller = controller;
    }

    public Task<bool> RunManualTaskAsync(string taskName)
    {
        return _controller.TryRunCustomTaskAsync(taskName, () => ExecuteTaskAsync(taskName));
    }

    private Task ExecuteTaskAsync(string taskName)
    {
        switch (taskName)
        {
            case "UpdateFileList":
                return DoRefreshFileListAsync();
            case "ScanDatabase":
                return DoScanDatabaseAsync();
            default:
                throw new InvalidOperationException($"Unknown task: {taskName}");
        }
    }

    private async Task DoRefreshFileListAsync()
    {
        await Task.Delay(5000); // имитация работы
    }

    private async Task DoScanDatabaseAsync()
    {
        await Task.Delay(7000); // имитация работы
    }
}