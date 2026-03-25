using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Modèle de message réutilisable avec variables de publipostage.
/// Variables : {{ownerName}}, {{unitReference}}, {{amount}}, {{dueDate}}, etc.
/// </summary>
public class MessageTemplate : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;

    /// <summary>Canal cible (un template par canal).</summary>
    public MessageChannel Channel { get; set; }

    public string? Subject { get; set; }
    public string Body { get; set; } = default!;

    /// <summary>Catégorie : convocation, relance, quittance, PV, etc.</summary>
    public string? Category { get; set; }

    /// <summary>Liste des variables disponibles (JSON array).</summary>
    public string? AvailableVariables { get; set; }

    public bool IsActive { get; set; } = true;
}
