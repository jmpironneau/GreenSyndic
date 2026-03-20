using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Physical building or entity (immeuble R+5, villa zone, COSMOS, etc.)
/// </summary>
public class Building : BaseEntity
{
    public Guid AppTenantId { get; set; }
    public AppTenant AppTenant { get; set; } = default!;

    public Guid? CoproprieteId { get; set; }
    public Copropriete? Copropriete { get; set; }

    public string Name { get; set; } = default!;              // "Acajou", "COSMOS", "Retail Park"
    public string? Code { get; set; }                          // Code court
    public PropertyType PrimaryType { get; set; }
    public string? Address { get; set; }
    public int? NumberOfFloors { get; set; }
    public decimal? TotalAreaSqm { get; set; }                 // Surface totale m²
    public decimal? CommonAreaSqm { get; set; }                // Parties communes m²
    public bool HasElevator { get; set; }
    public bool HasGenerator { get; set; }
    public bool HasParking { get; set; }
    public string? Description { get; set; }

    // Navigation
    public ICollection<Unit> Units { get; set; } = [];
}
