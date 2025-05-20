```c#
@page "/"

<div class="timeline-container">
    <div class="timeline-grid" style="grid-template-columns: repeat(@DaysInMonth.Count, 40px);">
        @foreach (var day in DaysInMonth)
        {
            <div class="timeline-date">@day.ToString("dd.MM")</div>
        }

        @for (int i = 0; i < RenderItems.Count; i++)
        {
            var (item, isSubtask) = RenderItems[i];

            @for (int d = 0; d < DaysInMonth.Count; d++)
            {
                <div class="timeline-cell"></div>
            }

            @if (TryGetGridColumns(item, out var colStart, out var colEnd))
            {
                <div class="timeline-bar @(isSubtask ? "subtask" : "")"
                     style="grid-column: @(colStart + 1) / @(colEnd + 2); grid-row: @(i + 2); background-color:@item.Color"
                     @onclick="() => OnBarClick(item)">
                    @item.Title
                </div>
            }
        }
    </div>
</div>

@code {
    private List<TaskItem> Tasks = new();

    private HashSet<Guid> ExpandedTasks = new();

    private int CurrentYear = DateTime.Today.Year;
    private int CurrentMonth = DateTime.Today.Month;

    private List<DateTime> DaysInMonth => Enumerable
        .Range(1, DateTime.DaysInMonth(CurrentYear, CurrentMonth))
        .Select(day => new DateTime(CurrentYear, CurrentMonth, day))
        .ToList();

    private List<(ITaskBound Item, bool IsSubtask)> RenderItems => BuildRenderItems();

    private List<(ITaskBound Item, bool IsSubtask)> BuildRenderItems()
    {
        var list = new List<(ITaskBound, bool)>();
        foreach (var task in Tasks)
        {
            list.Add((task, false));
            if (ExpandedTasks.Contains(task.Id))
            {
                foreach (var sub in task.SubTasks)
                    list.Add((sub, true));
            }
        }
        return list;
    }

    private void OnBarClick(ITaskBound item)
    {
        if (item is TaskItem task)
        {
            if (ExpandedTasks.Contains(task.Id))
                ExpandedTasks.Remove(task.Id);
            else
                ExpandedTasks.Add(task.Id);
        }
    }

    private bool TryGetGridColumns(ITaskBound item, out int startCol, out int endCol)
    {
        startCol = DaysInMonth.FindIndex(d => d.Date == item.StartDate.Date);
        endCol = DaysInMonth.FindIndex(d => d.Date == item.EndDate.Date);
        return startCol != -1 && endCol != -1;
    }

    protected override void OnInitialized()
    {
        // Для примера — загрузим тестовые задачи
        Tasks = SampleTasks();
    }

    private List<TaskItem> SampleTasks()
    {
        return new List<TaskItem>
        {
            new TaskItem
            {
                Title = "Проект А",
                Description = "Основная задача проекта А",
                StartDate = new DateTime(CurrentYear, CurrentMonth, 1),
                EndDate = new DateTime(CurrentYear, CurrentMonth, 5),
                Color = "#28a745",
                SubTasks = new List<SubTaskItem>
                {
                    new SubTaskItem
                    {
                        Title = "Анализ",
                        Description = "Подзадача анализа",
                        StartDate = new DateTime(CurrentYear, CurrentMonth, 2),
                        EndDate = new DateTime(CurrentYear, CurrentMonth, 3),
                        Color = "#20c997"
                    },
                    new SubTaskItem
                    {
                        Title = "Дизайн",
                        Description = "Подзадача дизайна",
                        StartDate = new DateTime(CurrentYear, CurrentMonth, 4),
                        EndDate = new DateTime(CurrentYear, CurrentMonth, 5),
                        Color = "#17a2b8"
                    }
                }
            },
            new TaskItem
            {
                Title = "Проект Б",
                Description = "Основная задача проекта Б",
                StartDate = new DateTime(CurrentYear, CurrentMonth, 10),
                EndDate = new DateTime(CurrentYear, CurrentMonth, 15),
                Color = "#dc3545"
            }
        };
    }
}
