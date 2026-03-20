namespace GreenSyndic.Core.Entities;

/// <summary>
/// Locataire / Lease tenant (residential or commercial).
/// </summary>
public class Locataire : BaseEntity
{
    public Guid AppTenantId { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? CompanyName { get; set; }                   // For commercial tenants
    public string? TradeName { get; set; }                     // Enseigne commerciale
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }                         // RCCM for commercial
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }

    // Link to Identity user
    public string? UserId { get; set; }

    // Navigation
    public ICollection<Lease> Leases { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
