using System;
using System.Collections.Generic;

namespace Jobick.ViewModels
{
    public class ProjectStatisticsVM
    {
        // First KPI counts
        public int InProgressProjects { get; set; }
        public int NotStartedProjects { get; set; }
        public int DoneProjects { get; set; }
        // NEW: Lists used for hover panels
        public List<ProjectStatusItem> InProgressProjectsList { get; set; } = new();
        public List<ProjectStatusItem> NotStartedProjectsList { get; set; } = new();
        public List<ProjectStatusItem> DoneProjectsList { get; set; } = new();

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

        // Sixth KPI - Progress Comparison (Targeted vs Actual)
        public Dictionary<int, decimal> TargetedProgressByYear { get; set; } = new();
        public Dictionary<int, decimal> ActualProgressByYear { get; set; } = new();

        // Quarterly Progress Comparison
        public Dictionary<string, decimal> TargetedProgressByQuarter { get; set; } = new();
        public Dictionary<string, decimal> ActualProgressByQuarter { get; set; } = new();

        // Detailed Project Progress by Quarter
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
            public string? DelayReasons { get; set; }
        }

        // NEW: Lightweight status item for KPI hover
        public class ProjectStatusItem
        {
            public int ProjectId { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}