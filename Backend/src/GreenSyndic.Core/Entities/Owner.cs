namespace GreenSyndic.Core.Entities;

/// <summary>
/// Copropriétaire / Property owner (~400 for Green City Bassam).
/// </summary>
public class Owner : BaseEntity
{
    public Guid AppTenantId { get; set; }

    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? CompanyName { get; set; }                   // For corporate owners
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? NationalId { get; set; }                    // CNI / Passeport
    public string? TaxId { get; set; }                         // NIF
    public bool IsCouncilMember { get; set; }
    public bool IsCouncilPresident { get; set; }
    public decimal Balance { get; set; }                       // Solde du compte

    // Link to Identity user (nullable - owner may not have login)
    public string? UserId { get; set; }

    // Navigation
    public ICollection<Unit> Units { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}
