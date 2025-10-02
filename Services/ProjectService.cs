using Jobick.Models;
using Microsoft.EntityFrameworkCore;

namespace Jobick.Services;

public class ProjectService
{
    private readonly AdhmmcPmContext _context;

    public ProjectService(AdhmmcPmContext context)
    {
        _context = context;
    }

    // Create (Async)
    public async System.Threading.Tasks.Task AddProjectAsync(Project project)
    {
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
    }

    // Create (Sync)
    internal void AddProject(Project project)
    {
        _context.Projects.Add(project);
        _context.SaveChanges();
    }

    // Read All (Async)
    public async Task<List<Project>> GetProjectListAsync()
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.CreatedByNavigation)
            .ToListAsync();
    }

    // Read Single (Sync)
    internal Project? GetProject(int id)
    {
        return _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.CreatedByNavigation)
            .FirstOrDefault(p => p.Id == id);
    }

    // Read Single (Async)
    public async Task<Project?> GetProjectAsync(int id)
    {
        return await _context.Projects
            .Include(p => p.Tasks)
            .Include(p => p.CreatedByNavigation)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Update (Async)
    public async System.Threading.Tasks.Task UpdateProjectAsync(Project project)
    {
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    // Update (Sync)
    internal void UpdateProject(Project project)
    {
        _context.Projects.Update(project);
        _context.SaveChanges();
    }

    // Delete (Async)
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

    // Delete (Sync)
    internal void DeleteProject(int id)
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

    internal bool ProjectExists(int id)
    {
        return _context.Projects.Any(p => p.Id == id);
    }
}
