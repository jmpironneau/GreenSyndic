using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Journal de livraison : chaque changement de statut d'un message.
/// </summary>
public class MessageDeliveryLog : BaseEntity
{
    public Guid MessageId { get; set; }
    public CommunicationMessage Message { get; set; } = default!;

    public DeliveryStatus Status { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Détail du fournisseur (webhook payload, bounce reason, etc.).</summary>
    public string? Details { get; set; }

    /// <summary>Identifiant externe (ex : SendGrid event ID).</summary>
    public string? ExternalEventId { get; set; }
}
