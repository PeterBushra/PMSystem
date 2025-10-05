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
    /// Deletes a project and its tasks asynchronously to maintain referential integrity.
    /// </summary>
    public async System.Threading.Tasks.Task DeleteProjectAsync(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project != null)
        {
            // Remove all linked tasks first
            _context.Tasks.RemoveRange(project.Tasks);
            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Deletes a project and its tasks synchronously.
    /// </summary>
    public void DeleteProject(int id)
    {
        var project = _context.Projects
            .Include(p => p.Tasks)
            .FirstOrDefault(p => p.Id == id);

        if (project != null)
        {
            // Remove all linked tasks first
            _context.Tasks.RemoveRange(project.Tasks);
            _context.Projects.Remove(project);
            _context.SaveChanges();
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
