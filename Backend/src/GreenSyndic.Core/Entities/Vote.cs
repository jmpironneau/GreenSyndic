using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Individual vote by an owner on a resolution.
/// </summary>
public class Vote : BaseEntity
{
    public Guid ResolutionId { get; set; }
    public Resolution Resolution { get; set; } = default!;

    public Guid OwnerId { get; set; }
    public Owner Owner { get; set; } = default!;

    public Guid? UnitId { get; set; }                          // Le lot au nom duquel il vote
    public VoteResult Result { get; set; }
    public decimal TantiemesWeight { get; set; }               // Poids du vote en tantièmes
    public bool IsProxy { get; set; }                          // Vote par procuration
    public Guid? ProxyOwnerId { get; set; }                    // Mandataire
}
