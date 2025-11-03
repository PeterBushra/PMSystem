using Jobick.Models;
using Microsoft.EntityFrameworkCore;
using Jobick.Services.Interfaces;

namespace Jobick.Services;

/// <summary>
/// Encapsulates data access for <see cref="Project"/> entities.
/// Keeps EF Core interaction in one place to support SRP and ease of maintenance.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly AdhmmcPmContext _context;

    public ProjectService(AdhmmcPmContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds a new project and saves changes asynchronously.
    /// </summary>
    public async System.Threading.Tasks.Task AddProjectAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a new project and saves changes synchronously.
    /// </summary>
    public void AddProject(Project project)
    {
        _context.Projects.Add(project);
        _context.SaveChanges();
    }

    /// <summary>
    /// Returns all projects including tasks and creator navigation properties.
    /// </summary>
    public async Task<List<Project>> GetProjectListAsync()
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.CreatedByNavigation)
            .ToListAsync();
    }

    /// <summary>
    /// Returns a single project by id including related entities.
    /// </summary>
    public Project? GetProject(int id)
    {
        return _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.CreatedByNavigation)
            .FirstOrDefault(p => p.Id == id);
    }

    /// <summary>
    /// Returns a single project by id including related entities asynchronously.
    /// </summary>
    public async Task<Project?> GetProjectAsync(int id)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.CreatedByNavigation)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// Updates a project asynchronously.
    /// </summary>
    public async System.Threading.Tasks.Task UpdateProjectAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a project synchronously.
    /// </summary>
    public void UpdateProject(Project project)
    {
        _context.Projects.Update(project);
        _context.SaveChanges();
    }

    /// <summary>
    /// Deletes a project and all its dependent data asynchronously.
    /// Relies on configured cascade deletes for TaskLogs and Tasks, but also handles
    /// pre-EF or mismatched DB schemas by explicitly removing dependents when needed.
    /// </summary>
    public async System.Threading.Tasks.Task DeleteProjectAsync(int id)
    {
        await using var tx = await _context.Database.BeginTransactionAsync();

        var project = await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project != null)
        {
            // Ensure TaskLogs are removed if database FK is not cascade
            var taskIds = project.Tasks.Select(t => t.Id).ToList();
            if (taskIds.Count > 0)
            {
                var logs = _context.TaskLogs.Where(l => taskIds.Contains(l.TaskId));
                _context.TaskLogs.RemoveRange(logs);
            }

            // Removing project will cascade Tasks in EF when configured; still safe if DB schema is older
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }

    /// <summary>
    /// Deletes a project and its tasks synchronously.
    /// </summary>
    public void DeleteProject(int id)
    {
        using var tx = _context.Database.BeginTransaction();

        var project = _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefault(p => p.Id == id);

        if (project != null)
        {
            var taskIds = project.Tasks.Select(t => t.Id).ToList();
            if (taskIds.Count > 0)
            {
                var logs = _context.TaskLogs.Where(l => taskIds.Contains(l.TaskId));
                _context.TaskLogs.RemoveRange(logs);
            }

            _context.Projects.Remove(project);
            _context.SaveChanges();
            tx.Commit();
        }
    }

    /// <summary>
    /// Checks existence of a project by id.
    /// </summary>
    public bool ProjectExists(int id)
    {
        return _context.Projects.Any(p => p.Id == id);
    }
}
