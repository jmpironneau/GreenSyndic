using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Point de l'ordre du jour d'une AG.
/// </summary>
public class MeetingAgendaItem : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = default!;

    public int OrderNumber { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public AgendaItemType Type { get; set; }

    /// <summary>Durée estimée en minutes.</summary>
    public int? EstimatedDurationMinutes { get; set; }

    /// <summary>Lien vers la résolution si ce point donne lieu à un vote.</summary>
    public Guid? ResolutionId { get; set; }
    public Resolution? Resolution { get; set; }

    /// <summary>Documents annexes (URLs séparées par ;).</summary>
    public string? AttachmentUrls { get; set; }
}
