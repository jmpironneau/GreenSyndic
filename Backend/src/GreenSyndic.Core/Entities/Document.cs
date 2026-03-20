using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// GED - Document management for the platform.
/// </summary>
public class Document : BaseEntity
{
    public Guid AppTenantId { get; set; }

    public string FileName { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string ContentType { get; set; } = default!;        // MIME type
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = default!;        // Path or blob URL
    public DocumentCategory Category { get; set; }
    public string? Description { get; set; }

    // Polymorphic link
    public Guid? UnitId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? MeetingId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? WorkOrderId { get; set; }
}
