using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Assemblée Générale or Council meeting (art. 388-391).
/// </summary>
public class Meeting : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid? CoOwnershipId { get; set; }
    public CoOwnership? CoOwnership { get; set; }

    public string Title { get; set; } = default!;
    public MeetingType Type { get; set; }
    public MeetingStatus Status { get; set; } = MeetingStatus.Planned;
    public DateTime ScheduledDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string? Location { get; set; }                      // Club House by default
    public string? ConvocationDocUrl { get; set; }
    public string? MinutesDocUrl { get; set; }                 // PV de l'AG
    public int? Quorum { get; set; }
    public int? AttendeesCount { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Resolution> Resolutions { get; set; } = [];
    public ICollection<MeetingAttendee> Attendees { get; set; } = [];
    public ICollection<MeetingAgendaItem> AgendaItems { get; set; } = [];
}
