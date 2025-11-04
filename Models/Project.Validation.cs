using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Jobick.Models;

public partial class Project : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // End date must be strictly greater than start date
        if (EndDate <= StartSate)
        {
            yield return new ValidationResult(
                "????? ???????? ??? ?? ???? ??? ????? ?????.",
                new[] { nameof(EndDate) }
            );
        }
    }
}
