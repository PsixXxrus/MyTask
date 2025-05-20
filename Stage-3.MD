```c#

<div class="timeline-container">
    <div class="timeline-grid">
        <!-- Заголовок -->
        <div class="timeline-header">
            <div class="timeline-spacer"></div>
            @foreach (var day in DaysInMonth)
            {
                <div class="timeline-date">@day:dd</div>
            }
        </div>

        <!-- Строки задач -->
        @foreach (var task in Tasks)
        {
            <div class="timeline-row">
                <div class="timeline-spacer"></div>

                @for (int i = 0; i < DaysInMonth.Count; i++)
                {
                    <div class="timeline-cell">
                        @if (DaysInMonth[i] >= task.StartDate.Date && DaysInMonth[i] <= task.EndDate.Date)
                        {
                            <div class="timeline-bar"
                                 style="background-color:@task.Color"
                                 @onclick="() => ToggleExpand(task.Id)">
                                @task.Title
                            </div>
                        }
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
                            <div class="timeline-cell">
                                @if (DaysInMonth[i] >= sub.StartDate.Date && DaysInMonth[i] <= sub.EndDate.Date)
                                {
                                    <div class="timeline-bar subtask">
                                        @sub.Title
                                    </div>
                                }
                            </div>
                        }
                    </div>
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
    display: flex;
    flex-direction: column;
    min-width: max-content;
    font-size: 0.85rem;
}

.timeline-header {
    display: flex;
}

.timeline-row {
    display: flex;
}

.timeline-spacer {
    width: 150px;
    min-width: 150px;
    border-right: 1px solid #dee2e6;
    background: #f0f0f0;
}

.timeline-date, .timeline-cell {
    width: 40px;
    min-width: 40px;
    height: 35px;
    border: 1px solid #dee2e6;
    text-align: center;
    box-sizing: border-box;
}

.timeline-date {
    background: #f8f9fa;
    font-weight: bold;
}

.timeline-bar {
    background-color: #007bff;
    color: white;
    width: 100%;
    height: 100%;
    font-size: 0.75rem;
    line-height: 1;
    padding: 3px;
    box-sizing: border-box;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    border-radius: 3px;
    cursor: pointer;
}

.timeline-bar.subtask {
    background-color: #6c757d;
}
