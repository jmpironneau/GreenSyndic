using GreenSyndic.Core.Enums;

namespace GreenSyndic.Services.DTOs;

// === Organization ===
public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? Country { get; set; }
    public string Currency { get; set; } = "XOF";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
}

public class CreateOrganizationRequest
{
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? Country { get; set; } = "CI";
    public string Currency { get; set; } = "XOF";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

// === Building ===
public class BuildingDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public PropertyType PrimaryType { get; set; }
    public string? Address { get; set; }
    public int? NumberOfFloors { get; set; }
    public decimal? TotalAreaSqm { get; set; }
    public decimal? CommonAreaSqm { get; set; }
    public bool HasElevator { get; set; }
    public bool HasGenerator { get; set; }
    public bool HasParking { get; set; }
    public string? Description { get; set; }
    public int UnitCount { get; set; }
}

public class CreateBuildingRequest
{
    public Guid OrganizationId { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public PropertyType PrimaryType { get; set; }
    public string? Address { get; set; }
    public int? NumberOfFloors { get; set; }
    public decimal? TotalAreaSqm { get; set; }
    public decimal? CommonAreaSqm { get; set; }
    public bool HasElevator { get; set; }
    public bool HasGenerator { get; set; }
    public bool HasParking { get; set; }
    public string? Description { get; set; }
}

// === Unit ===
public class UnitDto
{
    public Guid Id { get; set; }
    public Guid BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string Reference { get; set; } = default!;
    public string? Name { get; set; }
    public PropertyType Type { get; set; }
    public UnitStatus Status { get; set; }
    public int? Floor { get; set; }
    public decimal AreaSqm { get; set; }
    public decimal? ShareRatio { get; set; }
    public decimal? HorizontalShareRatio { get; set; }
    public int? NumberOfRooms { get; set; }
    public decimal? MarketValue { get; set; }
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
}

public class CreateUnitRequest
{
    public Guid BuildingId { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string Reference { get; set; } = default!;
    public string? Name { get; set; }
    public PropertyType Type { get; set; }
    public int? Floor { get; set; }
    public decimal AreaSqm { get; set; }
    public decimal? ShareRatio { get; set; }
    public decimal? HorizontalShareRatio { get; set; }
    public int? NumberOfRooms { get; set; }
    public decimal? MarketValue { get; set; }
    public Guid? OwnerId { get; set; }
}

// === Owner ===
public class OwnerDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName => $"{FirstName} {LastName}";
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public bool IsCouncilMember { get; set; }
    public bool IsCouncilPresident { get; set; }
    public decimal Balance { get; set; }
    public int UnitCount { get; set; }
}

public class CreateOwnerRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }
}
