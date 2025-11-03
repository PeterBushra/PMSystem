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

        // Helper: clamp value to [0,1]
        static decimal Clamp01(decimal v) => v < 0m ? 0m : (v > 1m ? 1m : v);

        // Helper: normalize progress possibly given as [0,100] to [0,1]
        static decimal NormalizeProgress(decimal v)
        {
            if (v < 0m) return 0m;
            // if > 1, assume it's a percentage (0..100)
            if (v > 1m) return Clamp01(v / 100m);
            return Clamp01(v);
        }

        // Helper: get latest known progress for task up to (and including) cutoff date
        // Prefer TaskLogs; if none, fall back to DoneRatio (used for yearly view)
        static decimal GetTaskProgressUpTo(Jobick.Models.Task task, DateOnly cutoff)
        {
            decimal? fromLogs = task.TaskLogs?
                .Where(l => l.Date <= cutoff)
                .OrderByDescending(l => l.Date)
                .Select(l => (decimal?)l.Progress)
                .FirstOrDefault();

            if (fromLogs.HasValue)
            {
                return NormalizeProgress(fromLogs.Value);
            }

            // Fallback to DoneRatio if there are no logs up to cutoff
            var dr = task.DoneRatio ?? 0m;
            return NormalizeProgress(dr);
        }

        // Helper: logs-only progress at a given cutoff (no fallback)
        static decimal GetTaskProgressFromLogsAt(Jobick.Models.Task task, DateOnly cutoff)
        {
            decimal? val = task.TaskLogs?
                .Where(l => l.Date <= cutoff)
                .OrderByDescending(l => l.Date)
                .Select(l => (decimal?)l.Progress)
                .FirstOrDefault();
            return val.HasValue ? NormalizeProgress(val.Value) : 0m;
        }

        // Helper: sum of normalized log entries within [start, end] inclusive, clamped to [0,1]
        static decimal SumTaskLogsInRange(Jobick.Models.Task task, DateOnly start, DateOnly end)
        {
            if (task.TaskLogs == null) return 0m;
            var sum = task.TaskLogs
                .Where(l => l.Date >= start && l.Date <= end)
                .Sum(l => NormalizeProgress(l.Progress));
            return Clamp01(sum);
        }

        // Helper: logs-only delta within a quarter (P(end) - P(before start)) clamped to [0,1]
        // Note: kept for reference, but quarterly actuals will use SumTaskLogsInRange per requirements.
        static decimal GetTaskQuarterDeltaFromLogs(Jobick.Models.Task task, DateOnly quarterStart, DateOnly quarterEnd)
        {
            var pBeforeStart = GetTaskProgressFromLogsAt(task, quarterStart.AddDays(-1));
            var pAtEnd = GetTaskProgressFromLogsAt(task, quarterEnd);
            var delta = pAtEnd - pBeforeStart;
            return delta > 0m ? Clamp01(delta) : 0m;
        }

        // Pre-group tasks by ExpectedEndDate year for targeted
        var tasksByYearLookup = allTasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .GroupBy(t => t.ExpectedEndDate.Year)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Determine years to consider based on ExpectedEndDate and TaskLog years
        var logYears = allTasks
            .Where(t => projectIds.Contains(t.ProjectId) && t.TaskLogs != null && t.TaskLogs.Any())
            .SelectMany(t => t.TaskLogs.Select(l => l.Date.Year))
            .ToHashSet();
        var allYears = tasksByYearLookup.Keys.Union(logYears).OrderBy(y => y).ToList();

        // Compute yearly targeted + actual (actual now sums all logs within the year)
        foreach (var year in allYears)
        {
            // Targeted yearly: per-project total weights for tasks ending this year
            var tasksInYear = tasksByYearLookup.GetValueOrDefault(year, new List<Jobick.Models.Task>());
            var projectTargetSums = tasksInYear
                .GroupBy(t => t.ProjectId)
                .ToDictionary(g => g.Key, g => g.Sum(t => (t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m)));

            var targetsForAllProjects = projectIds.Select(pid => projectTargetSums.GetValueOrDefault(pid, 0m)).ToList();
            targetedProgressByYear[year] = targetsForAllProjects.Any() ? targetsForAllProjects.Average() : 0m;

            // Actual yearly: sum of weight * (sum of log increments within the year) for ALL tasks of each project
            var yearStart = new DateOnly(year, 1, 1);
            var yearEnd = new DateOnly(year, 12, 31);
            var allProjectTasks = allTasks.Where(t => projectIds.Contains(t.ProjectId));

            var actualSumsByProjectForYear = allProjectTasks
                .GroupBy(t => t.ProjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(t => ((t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m)) * SumTaskLogsInRange(t, yearStart, yearEnd))
                );

            var actualsForAllProjectsYear = projectIds.Select(pid => actualSumsByProjectForYear.GetValueOrDefault(pid, 0m)).ToList();
            actualProgressByYear[year] = actualsForAllProjectsYear.Any() ? actualsForAllProjectsYear.Average() : 0m;
        }

        // Determine years to compute quarters for: union of ExpectedEndDate years and TaskLog years
        var yearsForQuarters = allYears;

        // Compute quarterly targeted (by ExpectedEndDate) and actual (sum of logs within the quarter) for each year
        foreach (var year in yearsForQuarters)
        {
            var tasksInYear = tasksByYearLookup.GetValueOrDefault(year, new List<Jobick.Models.Task>());

            for (int quarter = 1; quarter <= 4; quarter++)
            {
                var quarterKey = $"{year}-Q{quarter}";

                // Targeted per quarter (ExpectedEndDate basis)
                var quarterTasks = tasksInYear.Where(t => GetQuarter(t.ExpectedEndDate) == quarter).ToList();
                var quarterProjectTargetSums = quarterTasks
                    .GroupBy(t => t.ProjectId)
                    .ToDictionary(g => g.Key, g => g.Sum(t => (t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m)));
                var quarterTargetsForAllProjects = projectIds.Select(pid => quarterProjectTargetSums.GetValueOrDefault(pid, 0m)).ToList();
                var quarterTargeted = quarterTargetsForAllProjects.Any() ? quarterTargetsForAllProjects.Average() : 0m;

                // Actual per quarter: sum of log increments within this quarter across ALL tasks for these projects
                var startMonth = (quarter - 1) * 3 + 1;
                var endMonth = quarter * 3;
                var quarterStart = new DateOnly(year, startMonth, 1);
                var endDay = DateTime.DaysInMonth(year, endMonth);
                var quarterEnd = new DateOnly(year, endMonth, endDay);

                var allProjectTasks = allTasks.Where(t => projectIds.Contains(t.ProjectId));
                var quarterActualByProject = allProjectTasks
                    .GroupBy(t => t.ProjectId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(t => ((t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m)) * SumTaskLogsInRange(t, quarterStart, quarterEnd))
                    );
                var quarterActualsForAllProjects = projectIds.Select(pid => quarterActualByProject.GetValueOrDefault(pid, 0m)).ToList();
                var quarterActualAvg = quarterActualsForAllProjects.Any() ? quarterActualsForAllProjects.Average() : 0m;

                targetedProgressByQuarter[quarterKey] = quarterTargeted;
                actualProgressByQuarter[quarterKey] = quarterActualAvg;
            }
        }

        // NEW: Calculate detailed project progress for each project, year, and quarter
        var projectProgressDetails = new List<ProjectStatisticsVM.ProjectProgressDetail>();
        
        foreach (var project in projects)
        {
            var projectTasks = allTasks.Where(t => t.ProjectId == project.Id).ToList();
            if (!projectTasks.Any()) continue;

            var projectName = string.IsNullOrWhiteSpace(project.NameAr) ? project.Name : project.NameAr;

            // Determine years for details: union of ExpectedEndDate years and TaskLog years for this project
            var expectedYears = projectTasks.Select(t => t.ExpectedEndDate.Year).ToHashSet();
            var projectLogYears = projectTasks
                .Where(t => t.TaskLogs != null && t.TaskLogs.Any())
                .SelectMany(t => t.TaskLogs.Select(l => l.Date.Year))
                .ToHashSet();
            var yearsForDetails = expectedYears.Union(projectLogYears).OrderBy(y => y).ToList();

            foreach (var year in yearsForDetails)
            {
                var tasksInYear = projectTasks.Where(t => t.ExpectedEndDate.Year == year).ToList();

                // Calculate annual target for this project (by ExpectedEndDate in this year)
                var annualTarget = tasksInYear.Sum(t => (t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m));

                // Calculate quarterly breakdown
                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    var quarterKey = $"Q{quarter}";
                    var quarterTasks = tasksInYear.Where(t => GetQuarter(t.ExpectedEndDate) == quarter).ToList();

                    var quarterTarget = quarterTasks.Sum(t => (t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m));

                    // Actual for details: primary = sum logs within the quarter across ALL project tasks; fallback = weighted DoneRatio for quarter tasks
                    var startMonth = (quarter - 1) * 3 + 1;
                    var endMonth = quarter * 3;
                    var quarterStart = new DateOnly(year, startMonth, 1);
                    var endDay = DateTime.DaysInMonth(year, endMonth);
                    var quarterEnd = new DateOnly(year, endMonth, endDay);

                    var quarterActualLogs = projectTasks.Sum(t => ((t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m)) * SumTaskLogsInRange(t, quarterStart, quarterEnd));
                    var quarterActualFallback = quarterTasks.Sum(t => ((t.Weight ?? 0m) < 0m ? 0m : (t.Weight ?? 0m)) * NormalizeProgress(t.DoneRatio ?? 0m));
                    var quarterActual = quarterActualLogs > 0m ? quarterActualLogs : quarterActualFallback;

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
