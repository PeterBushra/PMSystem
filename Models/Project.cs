using System;
using System.Collections.Generic;

namespace Jobick.Models;

public partial class Project
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string NameAr { get; set; } = null!;

    public string? Description { get; set; }

    public string? DescriptionAr { get; set; }

    public string? ResponsibleForImplementing { get; set; }

    public string? SystemOwner { get; set; }

    public string? ProjectGoal { get; set; }

    public DateTime StartSate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal? TotalCost { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
