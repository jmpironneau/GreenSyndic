using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Modèle de résolution type (catalogue) réutilisable d'AG en AG.
/// </summary>
public class ResolutionTemplate : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ResolutionMajority DefaultMajority { get; set; }

    /// <summary>Catégorie : budget, travaux, mandat, divers...</summary>
    public string? Category { get; set; }

    /// <summary>Référence légale (ex : art. 388 CCH).</summary>
    public string? LegalReference { get; set; }

    /// <summary>Texte pré-rempli de la résolution.</summary>
    public string? TemplateText { get; set; }

    public bool IsActive { get; set; } = true;
}
