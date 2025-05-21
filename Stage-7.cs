using Microsoft.EntityFrameworkCore;
using YourAppNamespace.Data;
using YourAppNamespace.Models;

namespace YourAppNamespace.Services;

public class TaskService
{
    private readonly AppDbContext _db;

    public TaskService(AppDbContext db)
    {
        _db = db;
    }

    // Получение всех задач с подзадачами
    public async Task<List<TaskItem>> GetAllTasksAsync()
    {
        return await _db.Tasks
            .Include(t => t.SubTasks)
            .OrderBy(t => t.StartDate)
            .ToListAsync();
    }

    // Получение задачи по ID
    public async Task<TaskItem?> GetTaskByIdAsync(Guid id)
    {
        return await _db.Tasks
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // Добавление или обновление задачи
    public async Task SaveTaskAsync(TaskItem task)
    {
        if (task.Id == Guid.Empty)
        {
            task.Id = Guid.NewGuid();

            foreach (var sub in task.SubTasks)
            {
                sub.Id = Guid.NewGuid();
                sub.TaskId = task.Id;
            }

            _db.Tasks.Add(task);
            _db.SubTasks.AddRange(task.SubTasks);
        }
        else
        {
            _db.Tasks.Update(task);

            foreach (var sub in task.SubTasks)
            {
                if (sub.Id == Guid.Empty)
                {
                    sub.Id = Guid.NewGuid();
                    sub.TaskId = task.Id;
                    _db.SubTasks.Add(sub);
                }
                else
                {
                    _db.SubTasks.Update(sub);
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    // Удаление задачи с подзадачами
    public async Task DeleteTaskAsync(Guid taskId)
    {
        var task = await _db.Tasks
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task != null)
        {
            _db.SubTasks.RemoveRange(task.SubTasks);
            _db.Tasks.Remove(task);
            await _db.SaveChangesAsync();
        }
    }

    // Удаление подзадачи по ID
    public async Task DeleteSubTaskAsync(Guid subTaskId)
    {
        var subTask = await _db.SubTasks.FindAsync(subTaskId);
        if (subTask != null)
        {
            _db.SubTasks.Remove(subTask);
            await _db.SaveChangesAsync();
        }
    }
}

private async Task Save()
{
    if (Task == null) return;

    await TaskService.SaveTaskAsync(Task);
    await OnClose.InvokeAsync();
}

private async void RemoveSubTask(SubTaskItem subTask)
{
    Task?.SubTasks.Remove(subTask);

    if (subTask.Id != Guid.Empty)
        await TaskService.DeleteSubTaskAsync(subTask.Id);
}
