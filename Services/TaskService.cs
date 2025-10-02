using Jobick.Models;
using Microsoft.EntityFrameworkCore;

namespace Jobick.Services;

public class TaskService
{
    private readonly AdhmmcPmContext _context;

    public TaskService(AdhmmcPmContext context)
    {
        _context = context;
    }

    // Create
    public async System.Threading.Tasks.Task AddTaskAsync(Models.Task task)
    {
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    // Read (Get by Id)
    public async Task<Models.Task?> GetTaskAsync(int id)
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.CreatedByNavigation)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    // Read (Get all)
    public async Task<List<Models.Task>> GetTaskListAsync()
    {
        return await _context.Tasks
            .Include(t => t.Project)
            .Include(t => t.CreatedByNavigation)
            .ToListAsync();
    }

    // Update
    public async System.Threading.Tasks.Task UpdateTaskAsync(Models.Task task)
    {
        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();
    }

    // Delete
    public async System.Threading.Tasks.Task DeleteTaskAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }
}