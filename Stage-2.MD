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
