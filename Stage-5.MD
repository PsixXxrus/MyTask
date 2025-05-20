```c#
@page "/"

<div class="timeline-container">
    <div class="timeline-grid" style="grid-template-columns: repeat(@DaysInMonth.Count, 40px);">
        @foreach (var day in DaysInMonth)
        {
            <div class="timeline-date">@day.ToString("dd.MM")</div>
        }

        @for (int i = 0; i < RenderItems.Count; i++)
        {
            var (item, isSubtask) = RenderItems[i];

            @for (int d = 0; d < DaysInMonth.Count; d++)
            {
                <div class="timeline-cell"></div>
            }

            @if (TryGetGridColumns(item, out var colStart, out var colEnd))
            {
                <div class="timeline-bar @(isSubtask ? "subtask" : "")"
                     style="grid-column: @(colStart + 1) / @(colEnd + 2); grid-row: @(i + 2); background-color:@item.Color"
                     @onclick="() => OnBarClick(item)">
                    @item.Title
                </div>
            }
        }
    </div>
</div>

@code {
    private List<TaskItem> Tasks = new();

    private HashSet<Guid> ExpandedTasks = new();

    private int CurrentYear = DateTime.Today.Year;
    private int CurrentMonth = DateTime.Today.Month;

    private List<DateTime> DaysInMonth => Enumerable
        .Range(1, DateTime.DaysInMonth(CurrentYear, CurrentMonth))
        .Select(day => new DateTime(CurrentYear, CurrentMonth, day))
        .ToList();

    private List<(ITaskBound Item, bool IsSubtask)> RenderItems => BuildRenderItems();

    private List<(ITaskBound Item, bool IsSubtask)> BuildRenderItems()
    {
        var list = new List<(ITaskBound, bool)>();
        foreach (var task in Tasks)
        {
            list.Add((task, false));
            if (ExpandedTasks.Contains(task.Id))
            {
                foreach (var sub in task.SubTasks)
                    list.Add((sub, true));
            }
        }
        return list;
    }

    private void OnBarClick(ITaskBound item)
    {
        if (item is TaskItem task)
        {
            if (ExpandedTasks.Contains(task.Id))
                ExpandedTasks.Remove(task.Id);
            else
                ExpandedTasks.Add(task.Id);
        }
    }

    private bool TryGetGridColumns(ITaskBound item, out int startCol, out int endCol)
    {
        startCol = DaysInMonth.FindIndex(d => d.Date == item.StartDate.Date);
        endCol = DaysInMonth.FindIndex(d => d.Date == item.EndDate.Date);
        return startCol != -1 && endCol != -1;
    }

    protected override void OnInitialized()
    {
        // Для примера — загрузим тестовые задачи
        Tasks = SampleTasks();
    }

    private List<TaskItem> SampleTasks()
    {
        return new List<TaskItem>
        {
            new TaskItem
            {
                Title = "Проект А",
                Description = "Основная задача проекта А",
                StartDate = new DateTime(CurrentYear, CurrentMonth, 1),
                EndDate = new DateTime(CurrentYear, CurrentMonth, 5),
                Color = "#28a745",
                SubTasks = new List<SubTaskItem>
                {
                    new SubTaskItem
                    {
                        Title = "Анализ",
                        Description = "Подзадача анализа",
                        StartDate = new DateTime(CurrentYear, CurrentMonth, 2),
                        EndDate = new DateTime(CurrentYear, CurrentMonth, 3),
                        Color = "#20c997"
                    },
                    new SubTaskItem
                    {
                        Title = "Дизайн",
                        Description = "Подзадача дизайна",
                        StartDate = new DateTime(CurrentYear, CurrentMonth, 4),
                        EndDate = new DateTime(CurrentYear, CurrentMonth, 5),
                        Color = "#17a2b8"
                    }
                }
            },
            new TaskItem
            {
                Title = "Проект Б",
                Description = "Основная задача проекта Б",
                StartDate = new DateTime(CurrentYear, CurrentMonth, 10),
                EndDate = new DateTime(CurrentYear, CurrentMonth, 15),
                Color = "#dc3545"
            }
        };
    }
}




INSERT INTO Tasks (Id, Title, Description, StartDate, EndDate, Color) VALUES
('c982b1b9-6b01-4e43-8bb7-fdb26c8d4d61', 'Задача 1', 'Описание задачи 1', '2025-06-15', '2025-06-18', '#20c997'),
('cfef5819-0b44-471f-94cf-7939e621e7ef', 'Задача 2', 'Описание задачи 2', '2025-06-09', '2025-06-14', '#ffc107'),
('a2bcaef1-866b-4ad7-ad2a-186c03b37f7f', 'Задача 3', 'Описание задачи 3', '2025-06-15', '2025-06-19', '#fd7e14'),
('eaf32ad7-10c8-4a31-ac83-f86fd4f35bb4', 'Задача 4', 'Описание задачи 4', '2025-06-10', '2025-06-12', '#007bff'),
('beb68bde-373e-4bb7-9d73-1674118d4ed5', 'Задача 5', 'Описание задачи 5', '2025-06-03', '2025-06-08', '#28a745'),
('d09a3be3-b7de-4863-8f1a-8e9bbac9269d', 'Задача 6', 'Описание задачи 6', '2025-06-05', '2025-06-10', '#6f42c1'),
('5f3d938f-6b13-4a4d-a43a-9eddf7bc7337', 'Задача 7', 'Описание задачи 7', '2025-06-01', '2025-06-04', '#343a40'),
('e441dc93-d15f-49ea-8e99-97f929e1ad79', 'Задача 8', 'Описание задачи 8', '2025-06-06', '2025-06-09', '#6610f2'),
('8930f4e4-fc0f-42cb-a2d8-7f6f1914176b', 'Задача 9', 'Описание задачи 9', '2025-06-13', '2025-06-17', '#17a2b8'),
('de8d4b18-0a5a-41cb-8a69-929e94f528db', 'Задача 10', 'Описание задачи 10', '2025-06-04', '2025-06-06', '#dc3545');




INSERT INTO SubTasks (Id, TaskId, Title, Description, StartDate, EndDate, Color) VALUES
('492daac4-d1c1-4e15-a4c6-08240ed4e2f4', 'd09a3be3-b7de-4863-8f1a-8e9bbac9269d', 'Подзадача 1', 'Описание подзадачи 1', '2025-06-06', '2025-06-07', '#6c757d'),
('dae4fe65-f9f8-4129-b68d-742b8f3ab5c1', 'a2bcaef1-866b-4ad7-ad2a-186c03b37f7f', 'Подзадача 2', 'Описание подзадачи 2', '2025-06-16', '2025-06-17', '#17a2b8'),
('b582ae53-e340-410f-81ef-cb06a0390448', 'beb68bde-373e-4bb7-9d73-1674118d4ed5', 'Подзадача 3', 'Описание подзадачи 3', '2025-06-04', '2025-06-05', '#20c997'),
('abdbfc50-bc79-4d2e-9d86-3a33f81db585', 'eaf32ad7-10c8-4a31-ac83-f86fd4f35bb4', 'Подзадача 4', 'Описание подзадачи 4', '2025-06-11', '2025-06-12', '#0dcaf0'),
('db17a269-34c0-4ed4-8906-20f509e6f5de', 'c982b1b9-6b01-4e43-8bb7-fdb26c8d4d61', 'Подзадача 5', 'Описание подзадачи 5', '2025-06-16', '2025-06-17', '#adb5bd'),
('68e9b218-d14c-45e7-a308-5b9d71ebd05c', 'cfef5819-0b44-471f-94cf-7939e621e7ef', 'Подзадача 6', 'Описание подзадачи 6', '2025-06-12', '2025-06-13', '#17a2b8'),
('ef4bd50f-c60d-4e4e-b621-655f06db7589', '8930f4e4-fc0f-42cb-a2d8-7f6f1914176b', 'Подзадача 7', 'Описание подзадачи 7', '2025-06-14', '2025-06-15', '#0dcaf0'),
('8c7ed5ef-c85f-4219-9372-dde7adad11ee', '5f3d938f-6b13-4a4d-a43a-9eddf7bc7337', 'Подзадача 8', 'Описание подзадачи 8', '2025-06-02', '2025-06-03', '#adb5bd'),
('00ecb835-c9ea-474b-877e-823c15f110f4', 'e441dc93-d15f-49ea-8e99-97f929e1ad79', 'Подзадача 9', 'Описание подзадачи 9', '2025-06-07', '2025-06-08', '#17a2b8'),
('6aa51e61-f2b8-4e70-90f6-b9498e11047b', 'de8d4b18-0a5a-41cb-8a69-929e94f528db', 'Подзадача 10', 'Описание подзадачи 10', '2025-06-05', '2025-06-06', '#6c757d');



public class AppDbContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<SubTaskItem> SubTasks { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskItem>().HasKey(t => t.Id);
        modelBuilder.Entity<SubTaskItem>().HasKey(st => st.Id);

        modelBuilder.Entity<TaskItem>()
            .HasMany(t => t.SubTasks)
            .WithOne()
            .HasForeignKey(st => st.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}


@inject AppDbContext Db

@code {
    private List<TaskItem> Tasks = new();

    protected override async Task OnInitializedAsync()
    {
        Tasks = await Db.Tasks
            .Include(t => t.SubTasks)
            .ToListAsync();
    }
}




CREATE TABLE Tasks (
    Id TEXT PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    Color TEXT NOT NULL
);

CREATE TABLE SubTasks (
    Id TEXT PRIMARY KEY,
    TaskId TEXT NOT NULL,
    Title TEXT NOT NULL,
    Description TEXT,
    StartDate TEXT NOT NULL,
    EndDate TEXT NOT NULL,
    Color TEXT NOT NULL,
    FOREIGN KEY(TaskId) REFERENCES Tasks(Id)
);
