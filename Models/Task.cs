using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jobick.Models;

/// <summary>
/// EF Core entity representing a project task, including scheduling, progress, and costing.
/// </summary>
public partial class Task : IValidatableObject
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    [Required(ErrorMessage = "اسم المرحلة مطلوب")]
    [StringLength(500, ErrorMessage = "الحد الأقصى لطول اسم المرحلة هو 500 حرفًا")]
    public string? StageName { get; set; }

    [StringLength(500)]
    public string StageNameAr { get; set; } = null!;

    [Required(ErrorMessage = "المهمة مطلوبة")]
    [StringLength(500, ErrorMessage = "الحد الأقصى لطول اسم المهمة هو 500 حرفًا")]
    public string? Task1 { get; set; }

    [StringLength(500)]
    public string TaskAr { get; set; } = null!;

    [Required(ErrorMessage = "القسم المنفذ مطلوب")]
    [StringLength(500, ErrorMessage = "الحد الأقصى لطول اسم القسم المنفذ هو 500 حرفًا")]
    public string ImplementorDepartment { get; set; } = null!;

    [Required(ErrorMessage = "القسم المسؤول مطلوب")]
    [StringLength(500, ErrorMessage = "الحد الأقصى لطول الإدارة المسؤولة هو 500 حرفًا")]
    public string? DepartmentResponsible { get; set; }

    [Required(ErrorMessage = "تعريف الإنجاز مطلوب")]
    [StringLength(2000, ErrorMessage = "الحد الأقصى لطول خانة المخرجات هو 2000 حرف")]
    public string? DefinationOfDone { get; set; }

    [Required(ErrorMessage = "عدد الأيام لإكمال المهمة مطلوب")]
    public int ManyDaysToComplete { get; set; }

    [Required(ErrorMessage = "تاريخ البدء المتوقع مطلوب")]
    public DateTime ExpectedStartDate { get; set; }

    [Required(ErrorMessage = "تاريخ الانتهاء المتوقع مطلوب")]
    public DateTime ExpectedEndDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    [Required(ErrorMessage = "نسبة الإنجاز مطلوبة")]
    public decimal? DoneRatio { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    // Legacy fields (kept for backward compatibility; not used for storage going forward)
    public int? AttachmentFileName { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Project Project { get; set; } = null!;

    public decimal? Weight { get; set; }

    public decimal? Cost { get; set; }

    // Existing column with a typo - used as the backing store in the database
    public string? AttachementFilePath { get; set; }

    // New property for cleaner API; proxies to the existing DB column to avoid migrations
    [NotMapped]
    public string? AttachmentFilePath
    {
        get => AttachementFilePath;
        set => AttachementFilePath = value;
    }

    public virtual ICollection<TaskLog> TaskLogs { get; set; } = new List<TaskLog>();

    // Cross-field validation
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpectedEndDate <= ExpectedStartDate)
        {
            yield return new ValidationResult(
                "تاريخ الانتهاء يجب أن يكون بعد تاريخ البدء",
                new[] { nameof(ExpectedEndDate) }
            );
        }
    }
}
