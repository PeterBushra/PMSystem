using Jobick.Models;
using Jobick.ViewModels;
using static Jobick.Services.Statistics.StatisticsHelpers;

namespace Jobick.Services.Statistics;

internal static class ProgressCalculator
{
    public static ProgressAggregation Compute(
        IEnumerable<Project> projects,
        IEnumerable<Jobick.Models.Task> allTasks)
    {
        var result = new ProgressAggregation();

        var projectIds = projects.Select(p => p.Id).ToHashSet();

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

        // Compute yearly targeted + actual (actual sums all logs within the year)
        foreach (var year in allYears)
        {
            // Targeted yearly: per-project total weights for tasks ending this year
            var tasksInYear = tasksByYearLookup.GetValueOrDefault(year, new List<Jobick.Models.Task>());
            var projectTargetSums = tasksInYear
                .GroupBy(t => t.ProjectId)
                .ToDictionary(g => g.Key, g => g.Sum(t => SafeWeight(t.Weight)));

            var targetsForAllProjects = projectIds.Select(pid => projectTargetSums.GetValueOrDefault(pid, 0m)).ToList();
            result.TargetedByYear[year] = targetsForAllProjects.Any() ? targetsForAllProjects.Average() : 0m;

            // Actual yearly: sum of weight * (sum of log increments within the year) for ALL tasks of each project
            var yearStart = new DateOnly(year, 1, 1);
            var yearEnd = new DateOnly(year, 12, 31);
            var allProjectTasks = allTasks.Where(t => projectIds.Contains(t.ProjectId));

            var actualSumsByProjectForYear = allProjectTasks
                .GroupBy(t => t.ProjectId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(t => SafeWeight(t.Weight) * SumTaskLogsInRange(t, yearStart, yearEnd))
                );

            var actualsForAllProjectsYear = projectIds.Select(pid => actualSumsByProjectForYear.GetValueOrDefault(pid, 0m)).ToList();
            result.ActualByYear[year] = actualsForAllProjectsYear.Any() ? actualsForAllProjectsYear.Average() : 0m;
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
                    .ToDictionary(g => g.Key, g => g.Sum(t => SafeWeight(t.Weight)));
                var quarterTargetsForAllProjects = projectIds.Select(pid => quarterProjectTargetSums.GetValueOrDefault(pid, 0m)).ToList();
                var quarterTargeted = quarterTargetsForAllProjects.Any() ? quarterTargetsForAllProjects.Average() : 0m;

                // Actual per quarter: sum of log increments within this quarter across ALL tasks for these projects
                var (quarterStart, quarterEnd) = GetQuarterRange(year, quarter);

                var allProjectTasks = allTasks.Where(t => projectIds.Contains(t.ProjectId));
                var quarterActualByProject = allProjectTasks
                    .GroupBy(t => t.ProjectId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(t => SafeWeight(t.Weight) * SumTaskLogsInRange(t, quarterStart, quarterEnd))
                    );
                var quarterActualsForAllProjects = projectIds.Select(pid => quarterActualByProject.GetValueOrDefault(pid, 0m)).ToList();
                var quarterActualAvg = quarterActualsForAllProjects.Any() ? quarterActualsForAllProjects.Average() : 0m;

                result.TargetedByQuarter[quarterKey] = quarterTargeted;
                result.ActualByQuarter[quarterKey] = quarterActualAvg;
            }
        }

        // Detailed project progress for each project, year, and quarter
        foreach (var project in projects)
        {
            var projectTasks = allTasks.Where(t => t.ProjectId == project.Id).ToList();
            if (!projectTasks.Any()) continue;

            var projectName = string.IsNullOrWhiteSpace(project.NameAr) ? project.Name : project.NameAr;

            var expectedYears = projectTasks.Select(t => t.ExpectedEndDate.Year).ToHashSet();
            var projectLogYears = projectTasks
                .Where(t => t.TaskLogs != null && t.TaskLogs.Any())
                .SelectMany(t => t.TaskLogs.Select(l => l.Date.Year))
                .ToHashSet();
            var yearsForDetails = expectedYears.Union(projectLogYears).OrderBy(y => y).ToList();

            foreach (var year in yearsForDetails)
            {
                var tasksInYear = projectTasks.Where(t => t.ExpectedEndDate.Year == year).ToList();
                var annualTarget = tasksInYear.Sum(t => SafeWeight(t.Weight));

                for (int quarter = 1; quarter <= 4; quarter++)
                {
                    var quarterKey = $"Q{quarter}";
                    var quarterTasks = tasksInYear.Where(t => GetQuarter(t.ExpectedEndDate) == quarter).ToList();

                    var quarterTarget = quarterTasks.Sum(t => SafeWeight(t.Weight));

                    var (quarterStart, quarterEnd) = GetQuarterRange(year, quarter);

                    var quarterActualLogs = projectTasks.Sum(t => SafeWeight(t.Weight) * SumTaskLogsInRange(t, quarterStart, quarterEnd));
                    var quarterActualFallback = quarterTasks.Sum(t => SafeWeight(t.Weight) * NormalizeProgress(t.DoneRatio ?? 0m));
                    var quarterActual = quarterActualLogs > 0m ? quarterActualLogs : quarterActualFallback;

                    if (quarterTarget > 0 || quarterActual > 0)
                    {
                        result.ProjectDetails.Add(new ProjectStatisticsVM.ProjectProgressDetail
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

        return result;
    }
}
