# 📊 Компонент: График задач по времени (Blazor Server + Bootstrap)

---

## 📦 Файлы:
- `TaskTimeline.razor` — компонент графика
- `TaskCreator.razor` — компонент создания задачи (уже готов)
- `TaskService.cs` — сервис доступа к задачам
- `TaskItem`, `SubTaskItem` — модели данных

---

## 🎨 Компонент графика: `TaskTimeline.razor`

```razor
@inject TaskService TaskService

<h3>График задач</h3>

<button class="btn btn-success mb-3" @onclick="() => showCreateTask = !showCreateTask">
    @((showCreateTask ? "Скрыть" : "Создать новую задачу"))
</button>

@if (showCreateTask)
{
    <TaskCreator OnTaskCreated="ReloadTasks" />
}

<div class="timeline-container overflow-auto border p-2">
    <div class="timeline-grid">
        <!-- Шапка с датами -->
        <div class="timeline-header">
            @foreach (var day in DaysInMonth)
            {
                <div class="timeline-date text-center border-end">@day.ToString("dd")</div>
            }
        </div>

        <!-- Основные задачи -->
        @foreach (var task in Tasks)
        {
            <div class="timeline-row">
                <div class="task-title border-end">
                    <span @onclick="@(() => ToggleExpand(task.Id))" style="cursor: pointer;">
                        <strong>@task.Title</strong>
                        <span class="ms-1 text-secondary">@(!expandedTasks.Contains(task.Id) ? "▶" : "▼")</span>
                    </span>
                </div>

                @foreach (var day in DaysInMonth)
                {
                    var isInRange = day >= task.StartDate.Date && day <= task.EndDate.Date;
                    <div class="timeline-cell" style="@((isInRange ? $"background-color: {task.Color};" : ""))">
                    </div>
                }
            </div>

            <!-- Подзадачи -->
            @if (expandedTasks.Contains(task.Id))
            {
                foreach (var sub in task.SubTasks)
                {
                    <div class="timeline-row">
                        <div class="task-title border-end text-muted ps-4">
                            @sub.Title
                        </div>
                        @foreach (var day in DaysInMonth)
                        {
                            var isInRange = day >= sub.StartDate.Date && day <= sub.EndDate.Date;
                            <div class="timeline-cell" style="@((isInRange ? "background-color: #ccc;" : ""))"></div>
                        }
                    </div>
                }
            }
        }
    </div>
</div>

@code {
    private List<TaskItem> Tasks = new();
    private List<DateTime> DaysInMonth = new();
    private bool showCreateTask = false;
    private HashSet<int> expandedTasks = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        Tasks = await TaskService.GetTasksAsync();
        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        DaysInMonth = Enumerable.Range(0, (end - start).Days + 1)
            .Select(offset => start.AddDays(offset)).ToList();
    }

    private void ToggleExpand(int taskId)
    {
        if (expandedTasks.Contains(taskId))
            expandedTasks.Remove(taskId);
        else
            expandedTasks.Add(taskId);
    }

    private async Task ReloadTasks()
    {
        await LoadData();
        StateHasChanged();
    }
}


.timeline-container {
    max-width: 100%;
}

.timeline-grid {
    display: grid;
    grid-template-columns: 200px repeat(auto-fill, 40px);
    grid-auto-rows: minmax(30px, auto);
    font-size: 0.9rem;
}

.timeline-header {
    display: contents;
}

.timeline-date {
    padding: 5px;
    font-weight: bold;
    background: #f8f9fa;
}

.timeline-row {
    display: contents;
}

.task-title {
    background: #fff;
    padding: 5px;
    font-weight: 500;
    white-space: nowrap;
}

.timeline-cell {
    border: 1px solid #dee2e6;
    height: 30px;
}


@code {
    [Parameter]
    public EventCallback OnTaskCreated { get; set; }

    private async Task HandleValidSubmit()
    {
        await TaskService.AddTaskAsync(newTask);
        await OnTaskCreated.InvokeAsync(); // обновить родителя
        ...
    }
}

.timeline-container {
    overflow-x: auto;
    border: 1px solid #dee2e6;
}

.timeline-grid {
    display: grid;
    grid-template-columns: 200px repeat(31, 40px); /* по умолчанию 31 день, можно программно менять */
    grid-auto-rows: minmax(35px, auto);
    font-size: 0.9rem;
    min-width: max-content;
}

.timeline-header {
    display: contents;
}

.timeline-date {
    padding: 5px;
    font-weight: bold;
    background: #f8f9fa;
    text-align: center;
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
}

.timeline-row {
    display: contents;
}

.task-title {
    background: #fff;
    padding: 5px;
    font-weight: 500;
    white-space: nowrap;
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
}

.timeline-cell {
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    height: 35px;
    position: relative;
    padding: 0;
}

.timeline-bar {
    height: 100%;
    width: 100%;
    color: #fff;
    font-weight: 500;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 0.75rem;
    overflow: hidden;
    white-space: nowrap;
}

@foreach (var day in DaysInMonth)
{
    var isInRange = day >= task.StartDate.Date && day <= task.EndDate.Date;
    bool isStart = day == task.StartDate.Date;

    <div class="timeline-cell">
        @if (isInRange)
        {
            <div class="timeline-bar" style="background-color: @task.Color;">
                @if (isStart)
                {
                    @task.Title
                }
            </div>
        }
    </div>
}

@foreach (var day in DaysInMonth)
{
    var isInRange = day >= sub.StartDate.Date && day <= sub.EndDate.Date;
    bool isStart = day == sub.StartDate.Date;

    <div class="timeline-cell">
        @if (isInRange)
        {
            <div class="timeline-bar bg-secondary">
                @if (isStart)
                {
                    @sub.Title
                }
            </div>
        }
    </div>
}





----


@foreach (var task in Tasks)
{
    <div class="timeline-row">
        <div class="task-title border-end border-bottom"></div> <!-- Пустой заголовок -->

        @for (int i = 0; i < DaysInMonth.Count; i++)
        {
            <div class="timeline-cell"></div>
        }

        @if (TryGetSpan(task, out var colStart, out var colEnd))
        {
            <div class="timeline-bar"
                 style="background-color: @task.Color;"
                 style="@($"grid-column: {colStart} / {colEnd};")"
                 @onclick="@(() => ToggleExpand(task.Id))">
                @task.Title
            </div>
        }
    </div>

    @if (expandedTasks.Contains(task.Id))
    {
        foreach (var sub in task.SubTasks)
        {
            <div class="timeline-row">
                <div class="task-title border-end border-bottom"></div>

                @for (int i = 0; i < DaysInMonth.Count; i++)
                {
                    <div class="timeline-cell"></div>
                }

                @if (TryGetSpan(sub, out var sColStart, out var sColEnd))
                {
                    <div class="timeline-bar bg-secondary text-white"
                         style="@($"grid-column: {sColStart} / {sColEnd};")">
                        @sub.Title
                    </div>
                }
            </div>
        }
    }
}

private bool TryGetSpan(TaskBase task, out int colStart, out int colEnd)
{
    var startIndex = DaysInMonth.FindIndex(d => d.Date == task.StartDate.Date);
    var endIndex = DaysInMonth.FindIndex(d => d.Date == task.EndDate.Date);

    if (startIndex != -1 && endIndex != -1)
    {
        colStart = startIndex + 2; // +2, потому что первая колонка — заголовок задачи
        colEnd = endIndex + 3;
        return true;
    }

    colStart = colEnd = 0;
    return false;
}

.timeline-container {
    overflow-x: auto;
    border: 1px solid #dee2e6;
}

.timeline-grid {
    display: grid;
    grid-template-columns: 200px repeat(31, 40px); /* 31 день */
    grid-auto-rows: 35px;
    font-size: 0.85rem;
    min-width: max-content;
    position: relative;
}

.timeline-header {
    display: contents;
}

.timeline-date {
    padding: 5px;
    font-weight: bold;
    background: #f8f9fa;
    text-align: center;
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
}

.timeline-row {
    display: contents;
}

.task-title {
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    background: #fff;
}

.timeline-cell {
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    height: 35px;
}

.timeline-bar {
    position: absolute;
    height: 35px;
    display: flex;
    align-items: center;
    justify-content: start;
    padding-left: 5px;
    font-weight: 500;
    color: #fff;
    cursor: pointer;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    z-index: 1;
    border-radius: 3px;
}

@code {
    private List<TaskItem> Tasks = new();
    private List<DateTime> DaysInMonth = new();
    private HashSet<int> expandedTasks = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        Tasks = await TaskService.GetTasksAsync();

        var start = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        DaysInMonth = Enumerable.Range(0, (end - start).Days + 1)
            .Select(offset => start.AddDays(offset)).ToList();
    }

    private void ToggleExpand(int taskId)
    {
        if (expandedTasks.Contains(taskId))
            expandedTasks.Remove(taskId);
        else
            expandedTasks.Add(taskId);
    }
}



public interface ITimeBound
{
    DateTime StartDate { get; }
    DateTime EndDate { get; }
}


public class TaskItem : ITimeBound { /* ... */ }
public class SubTaskItem : ITimeBound { /* ... */ }





private bool TryGetSpan(ITimeBound item, out int colStart, out int colEnd)
{
    var startIndex = DaysInMonth.FindIndex(d => d.Date == item.StartDate.Date);
    var endIndex = DaysInMonth.FindIndex(d => d.Date == item.EndDate.Date);

    if (startIndex != -1 && endIndex != -1)
    {
        colStart = startIndex + 2;
        colEnd = endIndex + 3;
        return true;
    }

    colStart = colEnd = 0;
    return false;
}
