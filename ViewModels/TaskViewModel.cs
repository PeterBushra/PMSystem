using System.ComponentModel.DataAnnotations;

namespace Jobick.ViewModels;

public class TaskViewModel
{
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }

    [Display(Name = "Stage Name")]
    public string? StageName { get; set; }

    [Display(Name = "Stage Name (AR)")]
    public string StageNameAr { get; set; } = string.Empty;

    [Display(Name = "Task")]
    public string? Task1 { get; set; }

    [Display(Name = "Task (AR)")]
    public string TaskAr { get; set; } = string.Empty;

    [Display(Name = "Implementor Department")]
    public string ImplementorDepartment { get; set; } = string.Empty;

    [Display(Name = "Department Responsible")]
    public string? DepartmentResponsible { get; set; }

    [Display(Name = "Definition Of Done")]
    public string? DefinationOfDone { get; set; }

    [Display(Name = "Many Days To Complete")]
    public int ManyDaysToComplete { get; set; }

    [Display(Name = "Expected Start Date")]
    [DataType(DataType.Date)]
    public DateTime ExpectedStartDate { get; set; } = DateTime.Today;

    [Display(Name = "Expected End Date")]
    [DataType(DataType.Date)]
    public DateTime ExpectedEndDate { get; set; } = DateTime.Today.AddDays(1);

    [Display(Name = "Actual End Date")]
    [DataType(DataType.Date)]
    public DateTime ActualEndDate { get; set; } = DateTime.Today;

    [Display(Name = "Done Ratio (%)")]
    public decimal? DoneRatio { get; set; }
}
