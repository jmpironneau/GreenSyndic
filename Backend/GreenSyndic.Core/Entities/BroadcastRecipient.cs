namespace GreenSyndic.Core.Entities;

/// <summary>
/// Destinataire individuel d'un broadcast (pré-calculé ou ajouté manuellement).
/// </summary>
public class BroadcastRecipient : BaseEntity
{
    public Guid BroadcastId { get; set; }
    public Broadcast Broadcast { get; set; } = default!;

    /// <summary>UserId si destinataire interne.</summary>
    public string? UserId { get; set; }

    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }

    /// <summary>Variables de publipostage spécifiques (JSON object).</summary>
    public string? MergeData { get; set; }

    /// <summary>Lien vers le message généré.</summary>
    public Guid? MessageId { get; set; }
    public CommunicationMessage? Message { get; set; }
}
