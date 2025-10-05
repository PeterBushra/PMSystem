using Jobick.Models;

namespace Jobick.Services.Interfaces;

public interface IProjectService
{
    System.Threading.Tasks.Task AddProjectAsync(Project project);
    void AddProject(Project project);
    System.Threading.Tasks.Task<List<Project>> GetProjectListAsync();
    Project? GetProject(int id);
    System.Threading.Tasks.Task<Project?> GetProjectAsync(int id);
    System.Threading.Tasks.Task UpdateProjectAsync(Project project);
    void UpdateProject(Project project);
    System.Threading.Tasks.Task DeleteProjectAsync(int id);
    void DeleteProject(int id);
    bool ProjectExists(int id);
}
