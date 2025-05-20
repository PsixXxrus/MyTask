```c#

@inject ITaskService TaskService

<div class="timeline-container">
    <div class="timeline-grid">
        <!-- Заголовок: дни месяца -->
        <div class="timeline-header">
            <div class="timeline-spacer"></div>
            @foreach (var day in DaysInMonth)
            {
                <div class="timeline-date">@day.ToString("dd")</div>
            }
        </div>

        <!-- Строки задач -->
        @foreach (var task in Tasks)
        {
            <div class="timeline-row">
                <div class="timeline-spacer"></div>
                @for (int i = 0; i < DaysInMonth.Count; i++)
                {
                    <div class="timeline-cell"></div>
                }

                @if (TryGetSpan(task, out var colStart, out var colEnd))
                {
                    <div class="timeline-bar"
                         style="grid-column: @colStart / @colEnd; background-color:@task.Color;"
                         @onclick="@(() => ToggleExpand(task.Id))">
                        @task.Title
                    </div>
                }
            </div>

            <!-- Подзадачи -->
            @if (ExpandedTasks.Contains(task.Id))
            {
                foreach (var sub in task.SubTasks)
                {
                    <div class="timeline-row">
                        <div class="timeline-spacer"></div>
                        @for (int i = 0; i < DaysInMonth.Count; i++)
                        {
                            <div class="timeline-cell"></div>
                        }

                        @if (TryGetSpan(sub, out var subStart, out var subEnd))
                        {
                            <div class="timeline-bar subtask"
                                 style="grid-column: @subStart / @subEnd;">
                                @sub.Title
                            </div>
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
    private HashSet<int> ExpandedTasks = new();

    protected override async Task OnInitializedAsync()
    {
        var now = DateTime.Today;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        DaysInMonth = Enumerable.Range(0, (end - start).Days + 1)
            .Select(d => start.AddDays(d))
            .ToList();

        Tasks = await TaskService.GetTasksWithSubTasksAsync(start, end);
    }

    private void ToggleExpand(int taskId)
    {
        if (!ExpandedTasks.Add(taskId))
            ExpandedTasks.Remove(taskId);
    }

    private bool TryGetSpan(ITimeBound item, out int colStart, out int colEnd)
    {
        var startIndex = DaysInMonth.FindIndex(d => d.Date == item.StartDate.Date);
        var endIndex = DaysInMonth.FindIndex(d => d.Date == item.EndDate.Date);
        if (startIndex != -1 && endIndex != -1)
        {
            colStart = startIndex + 2; // +2 из-за первой колонки
            colEnd = endIndex + 3;
            return true;
        }

        colStart = colEnd = 0;
        return false;
    }
}


.timeline-container {
    overflow-x: auto;
    border: 1px solid #dee2e6;
    padding-bottom: 10px;
}

.timeline-grid {
    display: grid;
    grid-template-columns: 150px repeat(31, 40px);
    grid-auto-rows: 35px;
    position: relative;
    min-width: max-content;
    font-size: 0.85rem;
}

.timeline-header {
    display: contents;
}

.timeline-date {
    background: #f8f9fa;
    text-align: center;
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    font-weight: bold;
    padding: 5px 0;
}

.timeline-spacer {
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    background: #fff;
}

.timeline-row {
    display: contents;
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
    justify-content: center;
    font-weight: 600;
    color: #fff;
    z-index: 10;
    border-radius: 4px;
    cursor: pointer;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    padding: 0 6px;
}

.timeline-bar.subtask {
    height: 30px;
    font-size: 0.75rem;
    background-color: #6c757d !important;
}
