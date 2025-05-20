<div class="timeline-container">
    <div class="timeline-grid">
        <!-- Заголовок дат -->
        <div class="timeline-header">
            <div class="task-title"></div>
            @foreach (var day in DaysInMonth)
            {
                <div class="timeline-date">@day.ToString("dd")</div>
            }
        </div>

        <!-- Основные задачи -->
        @foreach (var task in Tasks)
        {
            <div class="timeline-row">
                <div class="task-title">@task.Title</div>
                @for (int i = 0; i < DaysInMonth.Count; i++)
                {
                    <div class="timeline-cell"></div>
                }

                @if (TryGetSpan(task, out var colStart, out var colEnd))
                {
                    <div class="timeline-bar"
                         style="grid-column: @colStart / @colEnd; background-color:@task.Color;"
                         @onclick="@(() => ToggleExpand(task.Id))">
                        &nbsp;
                    </div>
                }
            </div>

            @if (ExpandedTasks.Contains(task.Id))
            {
                foreach (var sub in task.SubTasks)
                {
                    <div class="timeline-row">
                        <div class="task-title">↳ @sub.Title</div>
                        @for (int i = 0; i < DaysInMonth.Count; i++)
                        {
                            <div class="timeline-cell"></div>
                        }

                        @if (TryGetSpan(sub, out var sStart, out var sEnd))
                        {
                            <div class="timeline-bar bg-secondary"
                                 style="grid-column: @sStart / @sEnd;">
                                &nbsp;
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

    protected override void OnInitialized()
    {
        LoadData();
    }

    private void LoadData()
    {
        var now = DateTime.Today;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        DaysInMonth = Enumerable.Range(0, (end - start).Days + 1)
            .Select(i => start.AddDays(i))
            .ToList();

        Tasks = TestData.GetSampleTasks(); // или из БД
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
            colStart = startIndex + 2; // +2 — из-за первой колонки (заголовок)
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
}

.timeline-grid {
    display: grid;
    grid-template-columns: 200px repeat(31, 40px);
    grid-auto-rows: 35px;
    position: relative;
    font-size: 0.85rem;
    min-width: max-content;
}

.timeline-header {
    display: contents;
}

.timeline-date {
    background: #f8f9fa;
    text-align: center;
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    padding: 5px;
    font-weight: bold;
}

.timeline-row {
    display: contents;
}

.task-title {
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    background: #fff;
    padding: 4px;
    white-space: nowrap;
}

.timeline-cell {
    border-right: 1px solid #dee2e6;
    border-bottom: 1px solid #dee2e6;
    height: 35px;
}

.timeline-bar {
    position: absolute;
    height: 35px;
    border-radius: 4px;
    z-index: 2;
    cursor: pointer;
}
