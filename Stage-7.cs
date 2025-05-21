@using YourAppNamespace.Models
@inject AppDbContext Db

@if (Visible && Task != null)
{
    <div class="modal fade show d-block" tabindex="-1" style="background-color: rgba(0,0,0,0.5);">
        <div class="modal-dialog modal-lg">
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
                    <div class="row mb-2">
                        <div class="col">
                            <label class="form-label">Дата начала</label>
                            <input type="date" class="form-control" @bind="Task.StartDate" />
                        </div>
                        <div class="col">
                            <label class="form-label">Дата окончания</label>
                            <input type="date" class="form-control" @bind="Task.EndDate" />
                        </div>
                    </div>

                    <hr />
                    <h6>Подзадачи</h6>
                    <button class="btn btn-sm btn-outline-primary mb-2" @onclick="AddSubTask">+ Подзадача</button>

                    @foreach (var sub in Task.SubTasks)
                    {
                        <div class="card mb-2">
                            <div class="card-body">
                                <div class="mb-1">
                                    <input class="form-control" @bind="sub.Title" placeholder="Название подзадачи" />
                                </div>
                                <div class="mb-1">
                                    <textarea class="form-control" @bind="sub.Description" placeholder="Описание подзадачи" />
                                </div>
                                <div class="row mb-1">
                                    <div class="col">
                                        <label>Начало</label>
                                        <input type="date" class="form-control" @bind="sub.StartDate" />
                                    </div>
                                    <div class="col">
                                        <label>Окончание</label>
                                        <input type="date" class="form-control" @bind="sub.EndDate" />
                                    </div>
                                </div>
                                <div class="d-flex justify-content-between align-items-center">
                                    <input type="color" class="form-control form-control-color" style="width: 50px;" @bind="sub.Color" />
                                    <button class="btn btn-sm btn-danger" @onclick="() => RemoveSubTask(sub)">Удалить</button>
                                </div>
                            </div>
                        </div>
                    }
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
            foreach (var sub in Task.SubTasks)
            {
                sub.Id = Guid.NewGuid();
                sub.TaskId = Task.Id;
            }

            Db.Tasks.Add(Task);
            Db.SubTasks.AddRange(Task.SubTasks);
        }
        else
        {
            Db.Tasks.Update(Task);

            foreach (var sub in Task.SubTasks)
            {
                if (sub.Id == Guid.Empty)
                {
                    sub.Id = Guid.NewGuid();
                    sub.TaskId = Task.Id;
                    Db.SubTasks.Add(sub);
                }
                else
                {
                    Db.SubTasks.Update(sub);
                }
            }
        }

        await Db.SaveChangesAsync();
        await OnClose.InvokeAsync();
    }

    private async Task Close()
    {
        Task = null;
        await OnClose.InvokeAsync();
    }

    private void AddSubTask()
    {
        Task?.SubTasks.Add(new SubTaskItem
        {
            Id = Guid.Empty,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Color = "#6c757d"
        });
    }

    private void RemoveSubTask(SubTaskItem subTask)
    {
        Task?.SubTasks.Remove(subTask);

        if (subTask.Id != Guid.Empty)
        {
            Db.SubTasks.Remove(subTask);
        }
    }
}
