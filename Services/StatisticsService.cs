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
        var today = now.Value.Date;

        int inProgress = 0, notStarted = 0, done = 0;
        foreach (var p in projects)
        {
            var tasks = (p.Tasks != null && p.Tasks.Count > 0)
                ? p.Tasks
                : allTasks.Where(t => t.ProjectId == p.Id).ToList();

            if (tasks == null || tasks.Count == 0)
                continue;

            bool allDone = tasks.All(t => (t.DoneRatio ?? 0m) >= 1.0m);
            bool noneStarted = tasks.All(t => (t.DoneRatio ?? 0m) == 0m);

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
            var tasks = (p.Tasks != null && p.Tasks.Count > 0)
                ? p.Tasks
                : allTasks.Where(t => t.ProjectId == p.Id).ToList();

            bool fullyDone = tasks.Count > 0 && tasks.All(t => (t.DoneRatio ?? 0m) >= 1.0m);
            if (fullyDone) continue;

            decimal budget = p.TotalCost ?? tasks.Sum(t => t.Cost ?? 0m);
            if (budget < 0) budget = 0;
            budgetsExceptFullyDone[p.Id] = budget;
            projectNames[p.Id] = string.IsNullOrWhiteSpace(p.NameAr) ? p.Name : p.NameAr;
        }

        var budgetsByYear = projects
            .GroupBy(p => p.EndDate.Year)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.TotalCost ?? 0m));

        // Overdue or At-Risk projects (with incomplete tasks)
        var overdueOrAtRisk = projects
            .Select(p => new
            {
                Project = p,
                Tasks = (p.Tasks != null && p.Tasks.Count > 0)
                    ? p.Tasks
                    : allTasks.Where(t => t.ProjectId == p.Id).ToList()
            })
            .Select(x =>
            {
                var incompleteTasks = x.Tasks.Where(t => (t.DoneRatio ?? 0m) < 1.0m).ToList();
                int incompleteCount = incompleteTasks.Count;

                // Total required days for remaining (incomplete) tasks
                int remainingRequiredDays = incompleteTasks.Sum(t => t.ManyDaysToComplete);

                // Available project duration in days (non-negative)
                int projectDurationDays = Math.Max(0, (x.Project.EndDate.Date - x.Project.StartSate.Date).Days);

                bool isOverdue = today > x.Project.EndDate.Date && incompleteCount > 0;
                bool isAtRisk = !isOverdue && incompleteCount > 0 && remainingRequiredDays > projectDurationDays;

                return new
                {
                    x.Project,
                    incompleteCount,
                    isOverdue,
                    isAtRisk
                };
            })
            .Where(x => x.isOverdue || x.isAtRisk)
            .Select(x => new ProjectStatisticsVM.ProjectInfo
            {
                ProjectId = x.Project.Id,
                Name = string.IsNullOrWhiteSpace(x.Project.NameAr) ? x.Project.Name : x.Project.NameAr,
                EndDate = x.Project.EndDate,
                IncompleteTasksCount = x.incompleteCount
            })
            // Keep overdue first if desired, then others by nearest end date
            .OrderByDescending(pi => pi.EndDate < today) // bool: overdue first
            .ThenBy(pi => pi.EndDate)
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
            OverdueProjectsWithIncompleteTasks = overdueOrAtRisk
        };
    }
}
