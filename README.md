# 📅 Blazor Server Планировщик Задач

Проект на **Blazor Server (.NET 7.0)** с **Bootstrap 5**, реализующий планирование и отображение задач на графике. Поддерживаются основные задачи и подзадачи, цветовая маркировка, а также отображение задач на временной шкале.

---

## ✅ Функциональность

- Основные задачи с описанием, сроками начала и окончания, и цветом.
- Подзадачи с описанием и отдельными сроками.
- Создание задач через UI.
- Отображение задач на графике (по дням, неделям, месяцам).
- Подзадачи раскрываются по кнопке.

---

## 🧩 Модель данных

### TaskItem.cs

```csharp
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Color { get; set; } = "#007bff"; // HEX цвет

    public List<SubTaskItem> SubTasks { get; set; } = new();
}

public class SubTaskItem
{
    public int Id { get; set; }
    public int TaskItemId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public TaskItem? TaskItem { get; set; }
}

public class PlannerDbContext : DbContext
{
    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<SubTaskItem> SubTasks { get; set; }
}

public class TaskService
{
    private readonly PlannerDbContext _context;

    public TaskService(PlannerDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaskItem>> GetTasksAsync()
    {
        return await _context.Tasks.Include(t => t.SubTasks).ToListAsync();
    }

    public async Task AddTaskAsync(TaskItem task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    // Дополнительно: Update, Delete и т.д.
}

public class TaskService
{
    private readonly PlannerDbContext _context;

    public TaskService(PlannerDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaskItem>> GetTasksAsync()
    {
        return await _context.Tasks.Include(t => t.SubTasks).ToListAsync();
    }

    public async Task AddTaskAsync(TaskItem task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    // Дополнительно: Update, Delete и т.д.
}

@inject TaskService TaskService

<h4>Создать задачу</h4>

<EditForm Model="@newTask" OnValidSubmit="HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label class="form-label">Название</label>
        <InputText class="form-control" @bind-Value="newTask.Title" />
    </div>

    <div class="mb-3">
        <label class="form-label">Описание</label>
        <InputTextArea class="form-control" @bind-Value="newTask.Description" />
    </div>

    <div class="row mb-3">
        <div class="col">
            <label class="form-label">Дата начала</label>
            <InputDate class="form-control" @bind-Value="newTask.StartDate" />
        </div>
        <div class="col">
            <label class="form-label">Дата окончания</label>
            <InputDate class="form-control" @bind-Value="newTask.EndDate" />
        </div>
    </div>

    <div class="mb-3">
        <label class="form-label">Цвет</label>
        <InputText class="form-control" type="color" @bind-Value="newTask.Color" />
    </div>

    <button type="submit" class="btn btn-primary">Добавить</button>
</EditForm>

@code {
    private TaskItem newTask = new()
    {
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddDays(1),
        Color = "#007bff"
    };

    private async Task HandleValidSubmit()
    {
        await TaskService.AddTaskAsync(newTask);
        newTask = new TaskItem
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1),
            Color = "#007bff"
        };
    }
}

builder.Services.AddDbContext<PlannerDbContext>(options =>
    options.UseInMemoryDatabase("PlannerDb"));

builder.Services.AddScoped<TaskService>();


@page "/tasks"
@inject TaskService TaskService

<h3>План задач</h3>

@foreach (var task in tasks)
{
    <div class="card my-3">
        <div class="card-header bg-primary text-white">
            <strong>@task.Title</strong> (@task.StartDate.ToShortDateString() - @task.EndDate.ToShortDateString())
        </div>
        <div class="card-body">
            <p>@task.Description</p>
            <ul class="list-group">
                @foreach (var sub in task.SubTasks)
                {
                    <li class="list-group-item">
                        <strong>@sub.Title:</strong> @sub.Description
                        <br />
                        <small>Срок: @sub.StartDate.ToShortDateString() - @sub.EndDate.ToShortDateString()</small>
                    </li>
                }
            </ul>
        </div>
    </div>
}

@code {
    private List<TaskItem> tasks = new();

    protected override async Task OnInitializedAsync()
    {
        tasks = await TaskService.GetTasksAsync();
    }
}

```
