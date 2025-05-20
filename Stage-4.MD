```c#

<div class="timeline-container">
    <div class="timeline-grid" style="grid-template-columns: 150px repeat(@DaysInMonth.Count, 40px);">

        <!-- Заголовок -->
        <div class="timeline-header">
            <div class="timeline-spacer"></div>
            @foreach (var day in DaysInMonth)
            {
                <div class="timeline-date">@day.ToString("dd.MM")</div>
            }
        </div>

        <!-- Основные задачи -->
        @for (int row = 0; row < Tasks.Count; row++)
        {
            var task = Tasks[row];
            <div class="timeline-spacer task-label">@($"Задача {task.Id}")</div>

            @for (int i = 0; i < DaysInMonth.Count; i++)
            {
                <div class="timeline-cell"></div>
            }

            @if (TryGetGridColumns(task, out var colStart, out var colEnd))
            {
                <div class="timeline-bar"
                     style="grid-column: @(colStart + 2) / @(colEnd + 3); grid-row: @(row + 2); background-color:@task.Color"
                     @onclick="() => ToggleExpand(task.Id)">
                    @task.Title
                </div>
            }

            <!-- Подзадачи -->
            if (ExpandedTasks.Contains(task.Id))
            {
                foreach (var sub in task.SubTasks)
                {
                    row++;
                    <div class="timeline-spacer subtask-label">@($"↳")</div>
                    @for (int i = 0; i < DaysInMonth.Count; i++)
                    {
                        <div class="timeline-cell"></div>
                    }

                    @if (TryGetGridColumns(sub, out var subStart, out var subEnd))
                    {
                        <div class="timeline-bar subtask"
                             style="grid-column: @(subStart + 2) / @(subEnd + 3); grid-row: @(row + 2);">
                            @sub.Title
                        </div>
                    }
                }
            }
        }
    </div>
</div>




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

.timeline-header {
    display: contents;
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

.timeline-spacer {
    border: 1px solid #dee2e6;
    background-color: #f0f0f0;
    text-align: right;
    padding-right: 5px;
    font-size: 0.75rem;
    line-height: 35px;
    font-weight: 500;
}

.task-label {
    background-color: #e9ecef;
}

.subtask-label {
    background-color: #f9f9f9;
}

.timeline-bar {
    position: relative;
    display: flex;
    align-items: center;
    justify-content: left;
    padding-left: 8px;
    color: #fff;
    font-weight: 600;
    font-size: 0.8rem;
    height: 35px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    border-radius: 4px;
    cursor: pointer;
    z-index: 10;
}

.timeline-bar.subtask {
    background-color: #6c757d !important;
}




private bool TryGetGridColumns(ITimeBound item, out int startCol, out int endCol)
{
    var start = DaysInMonth.FindIndex(d => d.Date == item.StartDate.Date);
    var end = DaysInMonth.FindIndex(d => d.Date == item.EndDate.Date);
    if (start != -1 && end != -1)
    {
        startCol = start;
        endCol = end;
        return true;
    }
    startCol = endCol = 0;
    return false;
}

//////

<div class="timeline-container">
    <div class="timeline-grid" style="grid-template-columns: 150px repeat(@DaysInMonth.Count, 40px);">
        
        <!-- Заголовок -->
        <div class="timeline-spacer"></div>
        @foreach (var day in DaysInMonth)
        {
            <div class="timeline-date">@day.ToString("dd.MM")</div>
        }

        @for (int taskRow = 0; taskRow < Tasks.Count; taskRow++)
        {
            var task = Tasks[taskRow];

            <!-- Пустая первая колонка (отступ слева) -->
            <div class="timeline-spacer"></div>

            <!-- Пустые ячейки сетки -->
            @for (int i = 0; i < DaysInMonth.Count; i++)
            {
                <div class="timeline-cell"></div>
            }

            <!-- Основная задача -->
            @if (TryGetGridColumns(task, out var colStart, out var colEnd))
            {
                <div class="timeline-bar"
                     style="grid-column: @(colStart + 2) / @(colEnd + 3); grid-row: @(taskRow + 2); background-color:@task.Color"
                     @onclick="() => ToggleExpand(task.Id)">
                    @task.Title
                </div>
            }

            <!-- Подзадачи -->
            if (ExpandedTasks.Contains(task.Id))
            {
                for (int subIndex = 0; subIndex < task.SubTasks.Count; subIndex++)
                {
                    var sub = task.SubTasks[subIndex];
                    var subRow = taskRow + subIndex + 1;

                    <!-- Пустая колонка -->
                    <div class="timeline-spacer"></div>

                    @for (int i = 0; i < DaysInMonth.Count; i++)
                    {
                        <div class="timeline-cell"></div>
                    }

                    @if (TryGetGridColumns(sub, out var subStart, out var subEnd))
                    {
                        <div class="timeline-bar subtask"
                             style="grid-column: @(subStart + 2) / @(subEnd + 3); grid-row: @(subRow + 2);">
                            @sub.Title
                        </div>
                    }
                }

                // Пропускаем строки под подзадачи
                taskRow += task.SubTasks.Count;
            }
        }
    </div>
</div>
