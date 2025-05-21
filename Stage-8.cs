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


<script src="js/task-interactions.js"></script>


@inject IJSRuntime JS
@inject TaskService TaskService

<div class="timeline-container">
    <div class="timeline-row" style="position: relative;">
        @foreach (var task in Tasks)
        {
            <div class="task-bar"
                 @ref="taskRefs[task.Id]"
                 style="@GetTaskStyle(task)"
                 @onmousedown="() => InitJsInterop(task)">
                @task.Title
            </div>
        }
    </div>
</div>

@code {
    private List<TaskItem> Tasks = new();
    private Dictionary<Guid, ElementReference> taskRefs = new();
    private int DayWidthPx = 30;
    private DateTime TimelineStartDate = new DateTime(2025, 5, 1);

    protected override async Task OnInitializedAsync()
    {
        Tasks = await TaskService.GetAllTasksAsync();
    }

    private string GetTaskStyle(TaskItem task)
    {
        var daysFromStart = (task.StartDate - TimelineStartDate).Days;
        var duration = (task.EndDate - task.StartDate).Days + 1;

        return $"left:{daysFromStart * DayWidthPx}px; " +
               $"width:{duration * DayWidthPx}px; " +
               $"top:{Tasks.IndexOf(task) * 40}px;";
    }

    private async Task InitJsInterop(TaskItem task)
    {
        var reference = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("taskTimeline.initResizableDraggable", taskRefs[task.Id], reference, DayWidthPx);
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
        Tasks = await TaskService.GetAllTasksAsync();
        StateHasChanged();
    }

    private TaskItem? SelectedTask;

    private void SetSelectedTask(TaskItem task)
    {
        SelectedTask = task;
    }
}






.timeline-container {
    position: relative;
    overflow-x: scroll;
    height: auto;
    border: 1px solid #ccc;
}

.timeline-row {
    position: relative;
    height: auto;
}

.task-bar {
    position: absolute;
    height: 30px;
    background-color: #007bff;
    color: white;
    padding: 4px;
    border-radius: 4px;
    cursor: grab;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
