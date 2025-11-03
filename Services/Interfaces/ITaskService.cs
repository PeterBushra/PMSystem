using Jobick.Models;

namespace Jobick.Services.Interfaces;

using EntityTask = Jobick.Models.Task;

public interface ITaskService
{
    System.Threading.Tasks.Task AddTaskAsync(EntityTask task);
    System.Threading.Tasks.Task<EntityTask?> GetTaskAsync(int id);
    System.Threading.Tasks.Task<System.Collections.Generic.List<EntityTask>> GetTaskListAsync();
    System.Threading.Tasks.Task UpdateTaskAsync(EntityTask task);
    System.Threading.Tasks.Task DeleteTaskAsync(int id);
    decimal GetTotalTasksWeights(int projectId);
    decimal GetTaskWeight(int taskID);
    // New helpers to avoid loading full task lists for simple aggregations
    decimal GetTotalTasksCost(int projectId);
    decimal GetTotalTasksCostExcluding(int projectId, int excludeTaskId);

    // Progress logs (timesheet-like)
    System.Threading.Tasks.Task<System.Collections.Generic.List<TaskLog>> GetTaskLogsAsync(int taskId);
    System.Threading.Tasks.Task ReplaceTaskLogsAsync(int taskId, System.Collections.Generic.IEnumerable<TaskLog> logs);
}
