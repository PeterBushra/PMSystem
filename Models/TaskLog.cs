using System;
using System.Collections.Generic;

namespace Jobick.Models;

public partial class TaskLog
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public decimal Progress { get; set; }

    public DateOnly Date { get; set; }

    public string? Notes { get; set; }

    public virtual Task Task { get; set; } = null!;
}
