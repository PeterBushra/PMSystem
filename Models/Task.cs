using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jobick.Models;

/// <summary>
/// EF Core entity representing a project task, including scheduling, progress, and costing.
/// </summary>
public partial class Task
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    [Required(ErrorMessage = "اسم المرحلة مطلوب")]
    public string? StageName { get; set; }

    public string StageNameAr { get; set; } = null!;

    [Required(ErrorMessage = "المهمة مطلوبة")]
    public string? Task1 { get; set; }

    public string TaskAr { get; set; } = null!;

    [Required(ErrorMessage = "القسم المنفذ مطلوب")]
    public string ImplementorDepartment { get; set; } = null!;

    [Required(ErrorMessage = "القسم المسؤول مطلوب")]
    public string? DepartmentResponsible { get; set; }

    [Required(ErrorMessage = "تعريف الإنجاز مطلوب")]
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

    public int? AttachmentFileName { get; set; }

    public byte[]? AttachmentData { get; set; }

    public string? AttachmentContentType { get; set; }
    public virtual User? CreatedByNavigation { get; set; }

    public virtual Project Project { get; set; } = null!;

    public decimal? Weight { get; set; }

    public decimal? Cost { get; set; }
    public string? AttachementFilePath { get; set; }

}
