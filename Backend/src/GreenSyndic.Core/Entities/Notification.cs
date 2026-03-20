namespace GreenSyndic.Core.Entities;

/// <summary>
/// In-app notifications for users.
/// </summary>
public class Notification : BaseEntity
{
    public Guid AppTenantId { get; set; }

    public string UserId { get; set; } = default!;             // Target user
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
