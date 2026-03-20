namespace GreenSyndic.Core.Entities;

/// <summary>
/// Prestataire / Fournisseur.
/// </summary>
public class Supplier : BaseEntity
{
    public Guid AppTenantId { get; set; }

    public string Name { get; set; } = default!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxId { get; set; }                         // RCCM
    public string? Specialty { get; set; }                     // "Plomberie", "Électricité", "Jardinage"
    public string? BankDetails { get; set; }
    public bool IsActive { get; set; } = true;

    // Link to Identity user
    public string? UserId { get; set; }

    // Navigation
    public ICollection<WorkOrder> WorkOrders { get; set; } = [];
}
