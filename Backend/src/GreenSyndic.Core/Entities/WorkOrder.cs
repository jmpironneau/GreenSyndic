using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Ordre de service / Work order for maintenance and repairs.
/// </summary>
public class WorkOrder : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public string Reference { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Draft;

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? IncidentId { get; set; }
    public Incident? Incident { get; set; }

    public Guid? BuildingId { get; set; }
    public Guid? UnitId { get; set; }

    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? Notes { get; set; }
}
