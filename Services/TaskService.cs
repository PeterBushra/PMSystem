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
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Retrieves all tasks with related entities.
    /// </summary>
    public async Task<List<Models.Task>> GetTaskListAsync()
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.CreatedByNavigation)
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
    /// Deletes a task by id if it exists.
    /// </summary>
    public async System.Threading.Tasks.Task DeleteTaskAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets the sum of weights for all tasks in a project.
    /// </summary>
    public decimal GetTotalTasksWeights(int projectId)
    {
        return _context.Tasks.Where(x=>x.ProjectId == projectId).Sum(x => x.Weight ?? 0);
    }

    /// <summary>
    /// Gets the weight value of a single task without tracking.
    /// </summary>
    public decimal GetTaskWeight(int taskID)
    {
        return _context.Tasks.AsNoTracking().FirstOrDefault(x => x.Id == taskID)?.Weight ?? 0;
    }
}