using Jobick.Models;
using Microsoft.EntityFrameworkCore;
using Jobick.Services.Interfaces;

namespace Jobick.Services;

/// <summary>
/// Provides CRUD operations and helpers for <see cref="Task"/> entities.
/// </summary>
public class TaskService : ITaskService
{
    private readonly AdhmmcPmContext _context;

    public TaskService(AdhmmcPmContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a new task and persists changes.
    /// </summary>
    public async System.Threading.Tasks.Task AddTaskAsync(Models.Task task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves a single task with related project and creator user by id.
    /// </summary>
    public async Task<Models.Task?> GetTaskAsync(int id)
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.CreatedByNavigation)
            .Include(t => t.TaskLogs)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Retrieves all tasks with related entities.
    /// </summary>
    public async Task<List<Models.Task>> GetTaskListAsync()
    {
        return await _context.Tasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.CreatedByNavigation)
            .Include(t => t.TaskLogs)
            .ToListAsync();
    }

    /// <summary>
    /// Updates a task and saves changes.
    /// </summary>
    public async System.Threading.Tasks.Task UpdateTaskAsync(Models.Task task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a task by id if it exists. Also deletes its TaskLog rows to satisfy FK_TaskLog_Task.
    /// </summary>
    public async System.Threading.Tasks.Task DeleteTaskAsync(int id)
    {
        // Use a transaction to ensure FK order safety
        await using var tx = await _context.Database.BeginTransactionAsync();

        // Remove dependent TaskLogs first (FK_TaskLog_Task has ClientSetNull/Restrict in DB)
        var logsQueryable = _context.TaskLogs.Where(l => l.TaskId == id);
        _context.TaskLogs.RemoveRange(logsQueryable);

        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>
    /// Gets the sum of weights for all tasks in a project.
    /// </summary>
    public decimal GetTotalTasksWeights(int projectId)
    {
        return _context.Tasks.AsNoTracking().Where(x => x.ProjectId == projectId).Sum(x => x.Weight ?? 0);
    }

    /// <summary>
    /// Gets the weight value of a single task without tracking.
    /// </summary>
    public decimal GetTaskWeight(int taskID)
    {
        return _context.Tasks.AsNoTracking().Where(x => x.Id == taskID).Select(x => x.Weight ?? 0).FirstOrDefault();
    }

    /// <summary>
    /// Gets the sum of costs for all tasks in a project.
    /// </summary>
    public decimal GetTotalTasksCost(int projectId)
    {
        return _context.Tasks.AsNoTracking().Where(x => x.ProjectId == projectId).Sum(x => x.Cost ?? 0);
    }

    /// <summary>
    /// Gets the sum of costs for all tasks in a project excluding a specific task.
    /// </summary>
    public decimal GetTotalTasksCostExcluding(int projectId, int excludeTaskId)
    {
        return _context.Tasks.AsNoTracking().Where(x => x.ProjectId == projectId && x.Id != excludeTaskId).Sum(x => x.Cost ?? 0);
    }

    // Task logs --------------------------------------------------------------

    /// <summary>
    /// Retrieves all logs for a specific task ordered by date.
    /// </summary>
    public async Task<List<TaskLog>> GetTaskLogsAsync(int taskId)
    {
        return await _context.TaskLogs
            .AsNoTracking()
            .Where(l => l.TaskId == taskId)
            .OrderBy(l => l.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Replaces the logs of a task atomically while avoiding tracking conflicts and honoring non-identity PK.
    /// </summary>
    public async System.Threading.Tasks.Task ReplaceTaskLogsAsync(int taskId, IEnumerable<TaskLog> logs)
    {
        // Use a transaction to keep the replacement consistent
        using var tx = await _context.Database.BeginTransactionAsync();

        // Delete existing logs by key-only instances and save to clear
        var existingIds = await _context.TaskLogs
            .Where(l => l.TaskId == taskId)
            .Select(l => l.Id)
            .ToListAsync();

        foreach (var id in existingIds)
        {
            _context.Entry(new TaskLog { Id = id }).State = EntityState.Deleted;
        }
        if (existingIds.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        // Determine next Id since TaskLog.Id is configured as ValueGeneratedNever
        int maxId = await _context.TaskLogs.Select(l => (int?)l.Id).MaxAsync() ?? 0;

        // Add new logs with unique Ids
        foreach (var l in logs)
        {
            var newLog = new TaskLog
            {
                Id = ++maxId,
                TaskId = taskId,
                Progress = Math.Clamp(l.Progress, 0m, 100m),
                Date = l.Date,
                Notes = l.Notes
            };
            _context.TaskLogs.Add(newLog);
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }
}