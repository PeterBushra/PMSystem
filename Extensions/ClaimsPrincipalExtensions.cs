using System.Security.Claims;

namespace Jobick.Extensions;

/// <summary>
/// Helper extensions for working with ClaimsPrincipal in controllers.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the numeric user id from the NameIdentifier claim or returns 0 if missing/invalid.
    /// </summary>
    public static int GetUserIdOrDefault(this ClaimsPrincipal user)
    {
        if (user is null) return 0;
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
}
