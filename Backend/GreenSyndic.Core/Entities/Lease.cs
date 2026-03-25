using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Bail (résidentiel CCH art. 414-450 ou commercial OHADA AUDCG art. 101-134).
/// </summary>
public class Lease : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = default!;

    public Guid LeaseTenantId { get; set; }
    public LeaseTenant LeaseTenant { get; set; } = default!;

    public string Reference { get; set; } = default!;         // Numéro du bail
    public LeaseType Type { get; set; }
    public LeaseStatus Status { get; set; } = LeaseStatus.Draft;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMonths { get; set; }

    public decimal MonthlyRent { get; set; }                   // Loyer mensuel FCFA
    public decimal? Charges { get; set; }                      // Charges récupérables
    public decimal SecurityDeposit { get; set; }               // Caution (max 2 mois art. 416)
    public decimal? AgencyFee { get; set; }                    // Frais d'agence

    // Rent revision (every 3 years - art. 421-424 / AUDCG art. 116)
    public DateTime? NextRevisionDate { get; set; }
    public decimal? RevisionIndexPercent { get; set; }

    // Commercial-specific
    public decimal? TurnoverRentPercent { get; set; }          // % CA pour centres commerciaux
    public decimal? MarketingContribution { get; set; }        // Fonds marketing COSMOS

    public string? Notes { get; set; }

    // Navigation
    public ICollection<Payment> Payments { get; set; } = [];
    public ICollection<RentCall> RentCalls { get; set; } = [];
    public ICollection<RentReceipt> RentReceipts { get; set; } = [];
    public ICollection<LeaseRevision> Revisions { get; set; } = [];
    public ICollection<ChargeRegularization> ChargeRegularizations { get; set; } = [];
}
