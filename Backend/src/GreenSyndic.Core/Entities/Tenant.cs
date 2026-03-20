namespace GreenSyndic.Core.Entities;

/// <summary>
/// Multi-tenant: represents a property development (e.g., Green City Bassam).
/// Not to be confused with a lease tenant (Locataire).
/// </summary>
public class AppTenant : BaseEntity
{
    public string Name { get; set; } = default!;         // "Green City Bassam"
    public string? LegalName { get; set; }               // Dénomination sociale
    public string? Country { get; set; }                 // "CI" (Côte d'Ivoire)
    public string Currency { get; set; } = "XOF";        // Franc CFA
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Copropriete> Coproprietes { get; set; } = [];
    public ICollection<Building> Buildings { get; set; } = [];
    public ICollection<Unit> Units { get; set; } = [];
}
