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

-- Создание таблицы TaskItem
CREATE TABLE TaskItem (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Description TEXT NOT NULL,
    StartDate TEXT NOT NULL,     -- Храним дату как строку в ISO 8601
    EndDate TEXT NOT NULL,
    Color TEXT NOT NULL
);

-- Создание таблицы SubTaskItem
CREATE TABLE SubTaskItem (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TaskItemId INTEGER NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT NOT NULL,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    FOREIGN KEY (TaskItemId) REFERENCES TaskItem(Id) ON DELETE CASCADE
);

-- Вставка 10 основных задач
INSERT INTO TaskItem (Title, Description, StartDate, EndDate, Color) VALUES 
('Разработка API', 'Создать REST API для взаимодействия', '2025-05-01', '2025-05-10', '#ff5733'),
('Дизайн интерфейса', 'Разработать макеты интерфейса пользователя', '2025-05-03', '2025-05-15', '#33c3ff'),
('Создание БД', 'Спроектировать структуру базы данных', '2025-05-05', '2025-05-08', '#28a745'),
('Интеграция оплаты', 'Подключение платежной системы', '2025-05-10', '2025-05-20', '#ffc107'),
('Тестирование', 'Провести модульное и интеграционное тестирование', '2025-05-15', '2025-05-25', '#6f42c1'),
('Настройка CI/CD', 'Настроить GitHub Actions и деплой', '2025-05-07', '2025-05-12', '#fd7e14'),
('Документация', 'Написать техническую документацию', '2025-05-08', '2025-05-18', '#20c997'),
('Анализ требований', 'Собрать и формализовать требования', '2025-05-01', '2025-05-04', '#dc3545'),
('UX тестирование', 'Провести тестирование с пользователями', '2025-05-18', '2025-05-24', '#0dcaf0'),
('Релиз v1.0', 'Подготовка и публикация первой версии', '2025-05-25', '2025-05-30', '#198754');

-- Вставка 10 подзадач (по 1–2 на задачу)
INSERT INTO SubTaskItem (TaskItemId, Title, Description, StartDate, EndDate) VALUES 
(1, 'Настройка контроллеров', 'Создание базовых контроллеров', '2025-05-01', '2025-05-03'),
(1, 'Маршрутизация', 'Настройка маршрутов в API', '2025-05-04', '2025-05-05'),
(2, 'Создание wireframes', 'Грубые наброски интерфейса', '2025-05-03', '2025-05-06'),
(2, 'Цветовая палитра', 'Выбор и тестирование цветов', '2025-05-07', '2025-05-08'),
(3, 'Определение сущностей', 'Выделить таблицы и связи', '2025-05-05', '2025-05-06'),
(4, 'Интеграция PayPal', 'Подключить и протестировать PayPal', '2025-05-10', '2025-05-12'),
(5, 'Unit-тесты', 'Покрытие логики unit-тестами', '2025-05-15', '2025-05-18'),
(6, 'Создание workflow', 'Настройка пайплайнов CI', '2025-05-07', '2025-05-09'),
(9, 'Обратная связь', 'Сбор отзывов пользователей', '2025-05-18', '2025-05-19'),
(10, 'Финальное ревью', 'Проверка релизной версии', '2025-05-28', '2025-05-29');
