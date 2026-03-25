using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Message individuel envoyé via un canal (email, SMS, push, courrier).
/// </summary>
public class CommunicationMessage : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary>Canal d'envoi.</summary>
    public MessageChannel Channel { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Draft;

    /// <summary>Destinataire : UserId (ASP.NET Identity) ou contact externe.</summary>
    public string? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientAddress { get; set; }

    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;

    /// <summary>Lien vers le template utilisé, si applicable.</summary>
    public Guid? TemplateId { get; set; }
    public MessageTemplate? Template { get; set; }

    /// <summary>Lien vers le broadcast parent, si envoi groupé.</summary>
    public Guid? BroadcastId { get; set; }
    public Broadcast? Broadcast { get; set; }

    /// <summary>Date d'envoi programmé (null = immédiat).</summary>
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }

    /// <summary>Identifiant externe du fournisseur (ex : SendGrid message ID).</summary>
    public string? ExternalId { get; set; }

    /// <summary>Erreur en cas d'échec.</summary>
    public string? ErrorMessage { get; set; }

    // Navigation
    public ICollection<MessageDeliveryLog> DeliveryLogs { get; set; } = [];
}
