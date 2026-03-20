using Microsoft.AspNetCore.Identity;

namespace GreenSyndic.Infrastructure.Identity;

/// <summary>
/// Custom Identity user with tenant support.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public Guid? AppTenantId { get; set; }
    public string? ProfileRole { get; set; }                   // Mapped to UserRole enum
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public string? PreferredLanguage { get; set; } = "fr";
}
