namespace GreenSyndic.Core.Entities;

/// <summary>
/// Charge assigned to a specific unit for a period.
/// </summary>
public class ChargeAssignment : BaseEntity
{
    public Guid ChargeDefinitionId { get; set; }
    public ChargeDefinition ChargeDefinition { get; set; } = default!;

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = default!;

    public int Year { get; set; }
    public int Quarter { get; set; }                           // 1-4
    public decimal Amount { get; set; }                        // Montant calculé pour ce lot
    public decimal PaidAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime DueDate { get; set; }
}
