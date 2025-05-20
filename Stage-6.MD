@using YourAppNamespace.Models
@inject AppDbContext Db

@if (Visible && Task != null)
{
    <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5);">
        <div class="modal-dialog">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title">@((IsNew ? "Создание задачи" : "Редактирование задачи"))</h5>
                    <button type="button" class="btn-close" @onclick="Close"></button>
                </div>
                <div class="modal-body">
                    <div class="mb-2">
                        <label class="form-label">Название</label>
                        <input class="form-control" @bind="Task.Title" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label">Описание</label>
                        <textarea class="form-control" @bind="Task.Description" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label">Цвет</label>
                        <input type="color" class="form-control form-control-color" @bind="Task.Color" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label">Дата начала</label>
                        <input type="date" class="form-control" @bind="Task.StartDate" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label">Дата окончания</label>
                        <input type="date" class="form-control" @bind="Task.EndDate" />
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn btn-secondary" @onclick="Close">Отмена</button>
                    <button class="btn btn-primary" @onclick="Save">Сохранить</button>
                </div>
            </div>
        </div>
    </div>
}

@code {
    [Parameter] public TaskItem? Task { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    private bool IsNew => Task?.Id == Guid.Empty;
    private bool Visible => Task != null;

    private async Task Save()
    {
        if (Task == null) return;

        if (IsNew)
        {
            Task.Id = Guid.NewGuid();
            Db.Tasks.Add(Task);
        }
        else
        {
            Db.Tasks.Update(Task);
        }

        await Db.SaveChangesAsync();
        await OnClose.InvokeAsync();
    }

    private async Task Close()
    {
        Task = null;
        await OnClose.InvokeAsync();
    }
}


<TaskEditor Task="SelectedTask" OnClose="CloseEditor" />



private TaskItem? SelectedTask;

private void OpenEditor(TaskItem task)
{
    SelectedTask = task;
}

private async Task CloseEditor()
{
    SelectedTask = null;
    await LoadTasks(); // перезагрузка
}


<div class="timeline-bar"
     style="... (позиция и цвет)"
     @ondblclick="@(() => OpenEditor(task))">
    @task.Title
</div>


<button class="btn btn-success mb-2" @onclick="CreateNewTask">Создать задачу</button>

@code {
    private void CreateNewTask()
    {
        SelectedTask = new TaskItem
        {
            Id = Guid.Empty,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Color = "#007bff"
        };
    }
}
