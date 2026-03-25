using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Envoi groupé / publipostage à un ensemble de destinataires.
/// </summary>
public class Broadcast : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public string Name { get; set; } = default!;
    public MessageChannel Channel { get; set; }
    public BroadcastStatus Status { get; set; } = BroadcastStatus.Draft;

    /// <summary>Template utilisé pour le publipostage.</summary>
    public Guid? TemplateId { get; set; }
    public MessageTemplate? Template { get; set; }

    /// <summary>Sujet (peut être surchargé par rapport au template).</summary>
    public string? Subject { get; set; }

    /// <summary>Corps (peut être surchargé par rapport au template).</summary>
    public string? Body { get; set; }

    /// <summary>Filtre destinataires : copropriété cible.</summary>
    public Guid? CoOwnershipId { get; set; }

    /// <summary>Filtre destinataires : rôle cible.</summary>
    public string? TargetRole { get; set; }

    /// <summary>Date d'envoi programmé.</summary>
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    // Navigation
    public ICollection<BroadcastRecipient> Recipients { get; set; } = [];
    public ICollection<CommunicationMessage> Messages { get; set; } = [];
}
