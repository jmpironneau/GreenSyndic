using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Resolution voted during an AG.
/// </summary>
public class Resolution : BaseEntity
{
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = default!;

    public int OrderNumber { get; set; }                       // Numéro d'ordre du jour
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ResolutionMajority RequiredMajority { get; set; }
    public int VotesFor { get; set; }
    public int VotesAgainst { get; set; }
    public int VotesAbstain { get; set; }
    public decimal TantiemesFor { get; set; }                  // Tantièmes pour
    public decimal TantiemesAgainst { get; set; }
    public bool IsApproved { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public ICollection<Vote> Votes { get; set; } = [];
}
