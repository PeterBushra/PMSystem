using Jobick.Models;
using Jobick.Services.Interfaces;
using Jobick.ViewModels;

namespace Jobick.Services;

/// <summary>
/// Aggregates dashboard-level KPIs across projects and tasks.
/// </summary>
public class StatisticsService : IStatisticsService
{
    public ProjectStatisticsVM CalculateDashboard(IEnumerable<Project> projects, IEnumerable<Jobick.Models.Task> allTasks, DateTime? now = null)
    {
        now ??= DateTime.Today;
        var today = now.Value;

        int inProgress = 0, notStarted = 0, done = 0;
        foreach (var p in projects)
        {
            if (p.Tasks == null || p.Tasks.Count == 0)
                continue;

            bool allDone = p.Tasks.All(t => t.DoneRatio == 1.0m);
            bool noneStarted = p.Tasks.All(t => (t.DoneRatio ?? 0m) == 0m);

            if (allDone) done++;
            else if (noneStarted) notStarted++;
            else inProgress++;
        }

        var projectsCountByYear = projects
            .GroupBy(p => p.EndDate.Year)
            .ToDictionary(g => g.Key, g => g.Count());

        var budgetsExceptFullyDone = new Dictionary<int, decimal>();
        var projectNames = new Dictionary<int, string>();

        foreach (var p in projects)
        {
            bool fullyDone = p.Tasks.Count > 0 && p.Tasks.All(t => t.DoneRatio == 1.0m);
            if (fullyDone) continue;

            decimal budget = p.TotalCost ?? p.Tasks.Sum(t => t.Cost ?? 0m);
            if (budget < 0) budget = 0;
            budgetsExceptFullyDone[p.Id] = budget;
            projectNames[p.Id] = p.Name;
        }

        var budgetsByYear = projects
            .GroupBy(p => p.EndDate.Year)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalCost ?? 0m));

        var overdueProjects = projects
            .Where(p => p.EndDate < today && p.Tasks.Any(t => t.DoneRatio < 1.0m))
            .Select(p => new ProjectStatisticsVM.ProjectInfo
            {
                ProjectId = p.Id,
                Name = p.Name,
                EndDate = p.EndDate,
                IncompleteTasksCount = p.Tasks.Count(t => t.DoneRatio < 1.0m)
            })
            .ToList();

        return new ProjectStatisticsVM
        {
            InProgressProjects = inProgress,
            NotStartedProjects = notStarted,
            DoneProjects = done,
            ProjectsCountByYear = projectsCountByYear,
            AllProjectsBudgetsExceptFullyDone = budgetsExceptFullyDone,
            ProjectNames = projectNames,
            ProjectsBudgetsByYear = budgetsByYear,
            OverdueProjectsWithIncompleteTasks = overdueProjects
        };
    }
}
