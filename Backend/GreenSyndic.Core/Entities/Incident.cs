using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Incident / Ticket reported by owner or tenant.
/// </summary>
public class Incident : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid? BuildingId { get; set; }
    public Building? Building { get; set; }

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IncidentPriority Priority { get; set; } = IncidentPriority.Medium;
    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;
    public string? Category { get; set; }                      // "Plomberie", "Électricité", etc.
    public string? ReportedByUserId { get; set; }
    public string? AssignedToUserId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? PhotoUrls { get; set; }                     // JSON array of photo URLs

    // Navigation
    public ICollection<WorkOrder> WorkOrders { get; set; } = [];
}
