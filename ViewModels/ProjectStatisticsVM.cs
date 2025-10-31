using System;
using System.Collections.Generic;

namespace Jobick.ViewModels
{
    public class ProjectStatisticsVM
    {
        // First KPI
        public int InProgressProjects { get; set; }
        public int NotStartedProjects { get; set; }
        public int DoneProjects { get; set; }

        // Second KPI
        public Dictionary<int, int> ProjectsCountByYear { get; set; } = new();

        // Third KPI (Budgets of not fully done projects)
        // ProjectId -> Budget value
        public Dictionary<int, decimal> AllProjectsBudgetsExceptFullyDone { get; set; } = new();
        // ProjectId -> Project Name (for labels)
        public Dictionary<int, string> ProjectNames { get; set; } = new();

        // Fourth KPI
        public Dictionary<int, decimal> ProjectsBudgetsByYear { get; set; } = new();

        // Fifth KPI
        public List<ProjectInfo> OverdueProjectsWithIncompleteTasks { get; set; } = new();

        // NEW: Sixth KPI - Progress Comparison (Targeted vs Actual)
        // Year -> Targeted Progress (sum of all task weights for tasks with ExpectedEndDate in that year)
        public Dictionary<int, decimal> TargetedProgressByYear { get; set; } = new();
        // Year -> Actual Progress (sum of weight * DoneRatio for tasks with ExpectedEndDate in that year)
        public Dictionary<int, decimal> ActualProgressByYear { get; set; } = new();

        // NEW: Quarterly Progress Comparison
        // Key format: "Year-Quarter" (e.g., "2025-Q1")
        public Dictionary<string, decimal> TargetedProgressByQuarter { get; set; } = new();
        public Dictionary<string, decimal> ActualProgressByQuarter { get; set; } = new();

        // NEW: Seventh KPI - Detailed Project Progress by Quarter
        // List of projects with their progress data for chart filtering
        public List<ProjectProgressDetail> ProjectProgressDetails { get; set; } = new();

        public class ProjectInfo
        {
            public int ProjectId { get; set; }
            public string Name { get; set; } = string.Empty;
            public DateTime EndDate { get; set; }
            public int IncompleteTasksCount { get; set; }
            public string? DelayReasons { get; set; }
        }

        public class ProjectProgressDetail
        {
            public int ProjectId { get; set; }
            public string ProjectName { get; set; } = string.Empty;
            public int Year { get; set; }
            public string Quarter { get; set; } = string.Empty; // Q1, Q2, Q3, Q4
            public decimal AnnualTargetProgress { get; set; }
            public decimal QuarterTargetProgress { get; set; }
            public decimal ActualProgress { get; set; }
            // NEW: Delay reasons for the project (to show in hover tooltip when below target)
            public string? DelayReasons { get; set; }
        }
    }
}