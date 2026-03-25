using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Feuille de présence AG : présence physique, visio ou par procuration.
/// </summary>
public class MeetingAttendee : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = default!;

    public Guid OwnerId { get; set; }
    public Owner Owner { get; set; } = default!;

    public AttendanceStatus Status { get; set; }

    /// <summary>Tantièmes représentés par ce participant.</summary>
    public decimal SharesRepresented { get; set; }

    /// <summary>Si présent par procuration, le mandataire.</summary>
    public Guid? ProxyHolderId { get; set; }

    /// <summary>Moyen par lequel la convocation a été envoyée.</summary>
    public ConvocationMethod? ConvocationMethod { get; set; }

    /// <summary>Date d'envoi de la convocation.</summary>
    public DateTime? ConvocationSentAt { get; set; }

    /// <summary>Date de réception/accusé de réception.</summary>
    public DateTime? ConvocationReceivedAt { get; set; }

    /// <summary>Signature numérique de la feuille de présence.</summary>
    public bool HasSigned { get; set; }

    public DateTime? SignedAt { get; set; }
}
