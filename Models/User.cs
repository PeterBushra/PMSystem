using System;
using System.Collections.Generic;

namespace Jobick.Models;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Password { get; set; }

    public bool Read { get; set; }

    public bool Write { get; set; }

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
