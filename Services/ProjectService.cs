using Jobick.Models;

namespace Jobick.Services;

public class ProjectService
{
    static List<Project> projects = new List<Project>
{
    new Project
    {
        Id = 1,
        Name = "Project A",
        NameAr = "مشروع أ",
        Description = "Description A",
        DescriptionAr = "وصف أ",
        ResponsibleForImplementing = "Team X",
        SystemOwner = "Owner Y",
        ProjectGoal = "Goal 1",
        StartSate= new DateTime(2025, 1, 1),
        EndDate = new DateTime(2025, 12, 31),
        TotalCost = 10000
    },
    new Project
    {
        Id = 2,
        Name = "Project B",
        NameAr = "مشروع ب",
        Description = "Description B",
        DescriptionAr = "وصف ب",
        ResponsibleForImplementing = "Team Z",
        SystemOwner = "Owner W",
        ProjectGoal = "Goal 2",
        StartSate = new DateTime(2025, 2, 1),
        EndDate = new DateTime(2025, 11, 30),
        TotalCost = 20000
    }
};

    internal void AddProject(Project project)
    {
        projects.Add(project);
    }

    internal List<Project> GetProjectList()
    {
        return projects;
    }
}
