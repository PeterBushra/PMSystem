using Jobick.ViewModels;

namespace Jobick.Services.Statistics;

internal sealed class ProgressAggregation
{
    public Dictionary<int, decimal> TargetedByYear { get; } = new();
    public Dictionary<int, decimal> ActualByYear { get; } = new();
    public Dictionary<string, decimal> TargetedByQuarter { get; } = new();
    public Dictionary<string, decimal> ActualByQuarter { get; } = new();
    public List<ProjectStatisticsVM.ProjectProgressDetail> ProjectDetails { get; } = new();
}
