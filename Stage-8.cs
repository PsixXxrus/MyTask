@inject TaskService TaskService
@inject IJSRuntime JS

<div class="timeline-container">
    <div class="timeline-header">
        @for (int i = 0; i < TotalDays; i++)
        {
            <div class="timeline-cell timeline-header-cell">
                @(TimelineStartDate.AddDays(i).ToString("dd.MM"))
            </div>
        }
    </div>

    @foreach (var task in Tasks)
    {
        <div class="timeline-row">
            <div class="task-bar"
                 @ref="taskRefs[task.Id]"
                 style="@GetTaskStyle(task)"
                 @onclick="() => SetSelectedTask(task)"
                 @onmousedown="() => InitJsInterop(task)">
                @task.Title
            </div>
        </div>

        @if (ExpandedTasks.Contains(task.Id))
        {
            foreach (var sub in task.SubTasks)
            {
                <div class="timeline-row subtasks-row">
                    <div class="task-bar subtask-bar"
                         style="@GetTaskStyle(sub)">
                        @sub.Title
                    </div>
                </div>
            }
        }
    }
</div>

@code {
    private List<TaskItem> Tasks = new();
    private Dictionary<Guid, ElementReference> taskRefs = new();
    private HashSet<Guid> ExpandedTasks = new();
    private TaskItem? SelectedTask;

    private int DayWidthPx = 30;
    private DateTime TimelineStartDate = new DateTime(2025, 5, 1);
    private int TotalDays = 30;

    protected override async Task OnInitializedAsync()
    {
        Tasks = await TaskService.GetAllTasksWithSubTasksAsync();
        taskRefs = Tasks.ToDictionary(t => t.Id, _ => default(ElementReference));
    }

    private string GetTaskStyle(ITaskBound task)
    {
        var offset = (task.StartDate - TimelineStartDate).Days;
        var span = (task.EndDate - task.StartDate).Days + 1;

        return $"left:{offset * DayWidthPx}px; width:{span * DayWidthPx}px; background-color:{task.Color};";
    }

    private void SetSelectedTask(TaskItem task)
    {
        SelectedTask = task;

        if (ExpandedTasks.Contains(task.Id))
            ExpandedTasks.Remove(task.Id);
        else
            ExpandedTasks.Add(task.Id);
    }

    private async Task InitJsInterop(TaskItem task)
    {
        SelectedTask = task;
        var dotNetRef = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("taskTimeline.initResizableDraggable", taskRefs[task.Id], dotNetRef, DayWidthPx);
    }

    [JSInvokable]
    public async Task OnDragOrResize(string direction, int deltaDays)
    {
        if (SelectedTask == null || deltaDays == 0) return;

        if (direction == "left")
            SelectedTask.StartDate = SelectedTask.StartDate.AddDays(deltaDays);
        else if (direction == "right")
            SelectedTask.EndDate = SelectedTask.EndDate.AddDays(deltaDays);
        else if (direction == "move")
        {
            SelectedTask.StartDate = SelectedTask.StartDate.AddDays(deltaDays);
            SelectedTask.EndDate = SelectedTask.EndDate.AddDays(deltaDays);
        }

        await TaskService.SaveTaskAsync(SelectedTask);
        Tasks = await TaskService.GetAllTasksWithSubTasksAsync();
        StateHasChanged();
    }
}


window.taskTimeline = {
    initResizableDraggable: function (element, dotNetRef, dayWidth) {
        const el = element;
        let startX = 0;
        let originalLeft = 0;
        let originalWidth = 0;
        let direction = null;

        el.onmousedown = function (e) {
            e.preventDefault();
            startX = e.clientX;
            originalLeft = el.offsetLeft;
            originalWidth = el.offsetWidth;

            const offsetX = e.offsetX;
            direction = offsetX < 10 ? 'left' : (offsetX > originalWidth - 10 ? 'right' : 'move');

            document.onmousemove = function (e) {
                const deltaX = e.clientX - startX;
                const deltaDays = Math.round(deltaX / dayWidth);

                dotNetRef.invokeMethodAsync("OnDragOrResize", direction, deltaDays);
            };

            document.onmouseup = function () {
                document.onmousemove = null;
                document.onmouseup = null;
            };
        };
    }
};




.timeline-container {
    overflow-x: auto;
    white-space: nowrap;
    border: 1px solid #ccc;
}

.timeline-header {
    display: flex;
    background-color: #f8f9fa;
    position: sticky;
    top: 0;
    z-index: 1;
}

.timeline-header-cell {
    min-width: 30px;
    height: 30px;
    text-align: center;
    border-right: 1px solid #ddd;
    font-size: 12px;
}

.timeline-row {
    position: relative;
    height: 35px;
    border-bottom: 1px solid #eee;
}

.task-bar {
    position: absolute;
    height: 30px;
    line-height: 30px;
    padding: 0 5px;
    color: white;
    border-radius: 4px;
    cursor: pointer;
    font-size: 12px;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.subtask-bar {
    opacity: 0.8;
    font-size: 11px;
    background-color: #6c757d !important;
}
