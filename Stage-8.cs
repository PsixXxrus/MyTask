@inject TaskService taskService
@inject IJSRuntime JS

<div class="timeline-container">
    <div class="timeline-header">
        @for (int i = 0; i < totalDays; i++)
        {
            <div class="timeline-cell timeline-header-cell">
                @(timelineStartDate.AddDays(i).ToString("dd.MM"))
            </div>
        }
    </div>

    @foreach (var task in tasks)
    {
        <div class="timeline-row">
            <div class="task-bar @(selectedTask?.Id == task.Id ? "selected" : "")"
                 @ref="taskRefs[task.Id]"
                 style="@GetTaskStyle(task)"
                 @onclick="() => ToggleTask(task)"
                 @ondblclick="() => EditTask(task)"
                 @onmousedown="() => InitJsInterop(task)">
                @task.Title
            </div>
        </div>

        @if (expandedTasks.Contains(task.Id))
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

<TaskEditor @bind-Visible="editorVisible"
            Task="editingTask"
            OnClose="CloseEditor"
            OnSaved="ReloadTasks" />

@code {
    private List<TaskItem> tasks = new();
    private Dictionary<Guid, ElementReference> taskRefs = new();
    private HashSet<Guid> expandedTasks = new();
    private TaskItem? selectedTask;
    private TaskItem? editingTask;
    private bool editorVisible = false;

    private int dayWidth = 30;
    private DateTime timelineStartDate = new DateTime(2025, 5, 1);
    private int totalDays = 30;

    protected override async Task OnInitializedAsync()
    {
        await ReloadTasks();
    }

    private async Task ReloadTasks()
    {
        tasks = await taskService.GetAllTasksAsync();
        taskRefs = tasks.ToDictionary(t => t.Id, _ => default(ElementReference));
        StateHasChanged();
    }

    private string GetTaskStyle(ITaskBound task)
    {
        var offset = (task.StartDate - timelineStartDate).Days;
        var span = (task.EndDate - task.StartDate).Days + 1;
        return $"left:{offset * dayWidth}px; width:{span * dayWidth}px; background-color:{task.Color};";
    }

    private void ToggleTask(TaskItem task)
    {
        if (selectedTask?.Id == task.Id)
            selectedTask = null;
        else
            selectedTask = task;

        if (expandedTasks.Contains(task.Id))
            expandedTasks.Remove(task.Id);
        else
            expandedTasks.Add(task.Id);
    }

    private void EditTask(TaskItem task)
    {
        editingTask = task;
        editorVisible = true;
    }

    private void CloseEditor()
    {
        editingTask = null;
        editorVisible = false;
    }

    private async Task InitJsInterop(TaskItem task)
    {
        selectedTask = task;
        var dotnetRef = DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("taskTimeline.initResizableDraggable", taskRefs[task.Id], dotnetRef, dayWidth);
    }

    [JSInvokable]
    public async Task OnDragOrResize(string direction, int deltaDays)
    {
        if (selectedTask == null || deltaDays == 0) return;

        if (direction == "left")
            selectedTask.StartDate = selectedTask.StartDate.AddDays(deltaDays);
        else if (direction == "right")
            selectedTask.EndDate = selectedTask.EndDate.AddDays(deltaDays);
        else if (direction == "move")
        {
            selectedTask.StartDate = selectedTask.StartDate.AddDays(deltaDays);
            selectedTask.EndDate = selectedTask.EndDate.AddDays(deltaDays);
        }

        await taskService.SaveTaskAsync(selectedTask);
        await ReloadTasks();
    }
}


window.taskTimeline = {
    initResizableDraggable: function (element, dotNetRef, dayWidth) {
        const el = element;
        let startX = 0;
        let direction = null;

        el.onmousedown = function (e) {
            e.preventDefault();
            startX = e.clientX;

            const offsetX = e.offsetX;
            const width = el.offsetWidth;

            if (offsetX < 10) direction = 'left';
            else if (offsetX > width - 10) direction = 'right';
            else direction = 'move';

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

.task-bar.selected {
    outline: 2px solid yellow;
}

.subtask-bar {
    opacity: 0.85;
    font-size: 11px;
    background-color: #6c757d !important;
}
