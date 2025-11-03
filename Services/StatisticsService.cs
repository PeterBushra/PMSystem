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

        // NEW: Calculate Progress Comparison (Targeted vs Actual)
        var targetedProgressByYear = new Dictionary<int, decimal>();
        var actualProgressByYear = new Dictionary<int, decimal>();
        var targetedProgressByQuarter = new Dictionary<string, decimal>();
        var actualProgressByQuarter = new Dictionary<string, decimal>();

        // Get all project IDs from the filtered projects
        var projectIds = projects.Select(p => p.Id).ToHashSet();

        // Pre-compute overall actual progress per project across all time (for yearly actuals requirement)
        var allTimeActualByProject = allTasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .GroupBy(t => t.ProjectId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(t => (t.Weight ?? 0m) * (t.DoneRatio ?? 0m))
            );

        // Group tasks by year (based on ExpectedEndDate) for projects in the filtered set
        var tasksByYear = allTasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .GroupBy(t => t.ExpectedEndDate.Year);

        foreach (var yearGroup in tasksByYear)
        {
            var year = yearGroup.Key;
            var tasksInYear = yearGroup.ToList();

            // Targeted Progress: compute per-project target sums for the year (ExpectedEndDate basis)
            var projectTargetSums = tasksInYear
                .GroupBy(t => t.ProjectId)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Weight ?? 0m));

            // For consistency average across the same set of projects (include zeros for projects without tasks in this year)
            var targetsForAllProjects = projectIds.Select(pid => projectTargetSums.GetValueOrDefault(pid, 0m)).ToList();
            var targetedProgress = targetsForAllProjects.Any() ? targetsForAllProjects.Average() : 0m;
            targetedProgressByYear[year] = targetedProgress;

            // Actual Progress (YEARLY VIEW): per-project actual sums across ALL time (ignores year)
            var allTimeActualsForProjects = projectIds.Select(pid => allTimeActualByProject.GetValueOrDefault(pid, 0m)).ToList();
            var actualProgressYearly = allTimeActualsForProjects.Any() ? allTimeActualsForProjects.Average() : 0m;
            actualProgressByYear[year] = actualProgressYearly;

            // Calculate quarterly breakdown for this year
            for (int quarter = 1; quarter <= 4; quarter++)
            {
                var quarterKey = $"{year}-Q{quarter}";

                // Targeted Progress (quarter): per-project target sums for this quarter (ExpectedEndDate basis)
                var quarterTasks = tasksInYear.Where(t => GetQuarter(t.ExpectedEndDate) == quarter).ToList();
                var quarterProjectTargetSums = quarterTasks
                    .GroupBy(t => t.ProjectId)
                    .ToDictionary(g => g.Key, g => g.Sum(t => t.Weight ?? 0m));

                var quarterTargetsForAllProjects = projectIds.Select(pid => quarterProjectTargetSums.GetValueOrDefault(pid, 0m)).ToList();
                var quarterTargeted = quarterTargetsForAllProjects.Any() ? quarterTargetsForAllProjects.Average() : 0m;

                // Actual Progress (QUARTERLY VIEW): cumulative up to this quarter based on ActualEndDate within the same year
                var actualYearTasks = allTasks
                    .Where(t => projectIds.Contains(t.ProjectId) && t.ActualEndDate.HasValue && t.ActualEndDate.Value.Year == year)
                    .ToList();

                var upToQuarterTasks = actualYearTasks
                    .Where(t => GetQuarter(t.ActualEndDate!.Value) <= quarter)
                    .ToList();

                var quarterCumulativeActualByProject = upToQuarterTasks
                    .GroupBy(t => t.ProjectId)
                    .ToDictionary(g => g.Key, g => g.Sum(t => (t.Weight ?? 0m) * (t.DoneRatio ?? 0m)));

                var quarterActualsForAllProjects = projectIds.Select(pid => quarterCumulativeActualByProject.GetValueOrDefault(pid, 0m)).ToList();
                var quarterActualAvg = quarterActualsForAllProjects.Any() ? quarterActualsForAllProjects.Average() : 0m;

                if (quarterTargeted > 0 || quarterActualAvg > 0)
                {
                    targetedProgressByQuarter[quarterKey] = quarterTargeted;
                    actualProgressByQuarter[quarterKey] = actualProgressYearly;
                }
            }
        }

        // NEW: Calculate detailed project progress for each project, year, and quarter
        var projectProgressDetails = new List<ProjectStatisticsVM.ProjectProgressDetail>();
        
        foreach (var project in projects)
        {
            var projectTasks = allTasks.Where(t => t.ProjectId == project.Id).ToList();
            if (!projectTasks.Any()) continue;

            var projectName = string.IsNullOrWhiteSpace(project.NameAr) ? project.Name : project.NameAr;

            // Group project tasks by year
            var projectTasksByYear = projectTasks.GroupBy(t => t.ExpectedEndDate.Year);

            foreach (var yearGroup in projectTasksByYear)
            {
                var year = yearGroup.Key;
                var tasksInYear = yearGroup.ToList();

                // Calculate annual target for this project
                var annualTarget = tasksInYear.Sum(t => t.Weight ?? 0m);

                // Calculate quarterly breakdown
                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    var quarterKey = $"Q{quarter}";
                    var quarterTasks = tasksInYear.Where(t => GetQuarter(t.ExpectedEndDate) == quarter).ToList();

                    var quarterTarget = quarterTasks.Sum(t => t.Weight ?? 0m);
                    var quarterActual = quarterTasks.Sum(t => (t.Weight ?? 0m) * (t.DoneRatio ?? 0m));

                    // Only add if there's data for this quarter
                    if (quarterTarget > 0 || quarterActual > 0)
                    {
                        projectProgressDetails.Add(new ProjectStatisticsVM.ProjectProgressDetail
                        {
                            ProjectId = project.Id,
                            ProjectName = projectName,
                            Year = year,
                            Quarter = quarterKey,
                            AnnualTargetProgress = annualTarget,
                            QuarterTargetProgress = quarterTarget,
                            ActualProgress = quarterActual,
                            DelayReasons = project.DelayReasons
                        });
                    }
                }
            }
        }

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
                IncompleteTasksCount = x.incompleteCount,
                DelayReasons = x.Project.DelayReasons
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
            OverdueProjectsWithIncompleteTasks = overdueOrAtRisk,
            TargetedProgressByYear = targetedProgressByYear,
            ActualProgressByYear = actualProgressByYear,
            TargetedProgressByQuarter = targetedProgressByQuarter,
            ActualProgressByQuarter = actualProgressByQuarter,
            ProjectProgressDetails = projectProgressDetails
        };
    }

    private static int GetQuarter(DateTime date)
    {
        return (date.Month - 1) / 3 + 1;
    }
}
