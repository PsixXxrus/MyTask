```c#

<div class="timeline-container">
    <div class="timeline-grid" style="grid-template-columns: repeat(@DaysInMonth.Count, 40px);">
        
        <!-- Заголовок -->
        @foreach (var day in DaysInMonth)
        {
            <div class="timeline-date">@day.ToString("dd.MM")</div>
        }

        @for (int i = 0; i < RenderItems.Count; i++)
        {
            var item = RenderItems[i];

            @for (int d = 0; d < DaysInMonth.Count; d++)
            {
                <div class="timeline-cell"></div>
            }

            @if (TryGetGridColumns(item, out var colStart, out var colEnd))
            {
                <div class="timeline-bar @(item is SubTaskItem ? "subtask" : "")"
                     style="grid-column: @(colStart + 1) / @(colEnd + 2); grid-row: @(i + 2); background-color:@item.Color"
                     @onclick="() => OnBarClick(item)">
                    @item.Title
                </div>
            }
        }
    </div>
</div>


@code {
    private List<DateTime> DaysInMonth = Enumerable.Range(1, DateTime.DaysInMonth(CurrentYear, CurrentMonth))
        .Select(day => new DateTime(CurrentYear, CurrentMonth, day)).ToList();

    private List<TaskItem> Tasks = new();
    private HashSet<Guid> ExpandedTasks = new();

    private int CurrentMonth = DateTime.Today.Month;
    private int CurrentYear = DateTime.Today.Year;

    private List<ITimeBound> RenderItems => GetRenderItems();

    private List<ITimeBound> GetRenderItems()
    {
        var items = new List<ITimeBound>();
        foreach (var task in Tasks)
        {
            items.Add(task);
            if (ExpandedTasks.Contains(task.Id))
            {
                items.AddRange(task.SubTasks);
            }
        }
        return items;
    }

    private void OnBarClick(ITimeBound item)
    {
        if (item is TaskItem task)
        {
            if (ExpandedTasks.Contains(task.Id))
                ExpandedTasks.Remove(task.Id);
            else
                ExpandedTasks.Add(task.Id);
        }
    }

    private bool TryGetGridColumns(ITimeBound item, out int startCol, out int endCol)
    {
        startCol = DaysInMonth.FindIndex(d => d.Date == item.StartDate.Date);
        endCol = DaysInMonth.FindIndex(d => d.Date == item.EndDate.Date);
        return startCol != -1 && endCol != -1;
    }
}

public interface ITimeBound
{
    Guid Id { get; }
    string Title { get; }
    DateTime StartDate { get; }
    DateTime EndDate { get; }
    string Color { get; }
}

.timeline-container {
    overflow-x: auto;
    border: 1px solid #dee2e6;
}

.timeline-grid {
    display: grid;
    grid-auto-rows: 35px;
    min-width: max-content;
    position: relative;
}

.timeline-date {
    border: 1px solid #dee2e6;
    background: #f8f9fa;
    text-align: center;
    font-weight: bold;
    font-size: 0.75rem;
    line-height: 35px;
}

.timeline-cell {
    border: 1px solid #dee2e6;
    background-color: #fff;
}

.timeline-bar {
    display: flex;
    align-items: center;
    padding-left: 8px;
    color: #fff;
    font-weight: 500;
    font-size: 0.8rem;
    height: 35px;
    overflow: hidden;
    white-space: nowrap;
    border-radius: 4px;
    cursor: pointer;
    z-index: 10;
}

.timeline-bar.subtask {
    background-color: #6c757d !important;
}
