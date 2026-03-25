using GreenSyndic.Core.Enums;

namespace GreenSyndic.Services.DTOs;

// === CoOwnership ===
public class CoOwnershipDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public CoOwnershipLevel Level { get; set; }
    public string? Description { get; set; }
    public string? RegulationReference { get; set; }
    public decimal AnnualBudget { get; set; }
    public decimal ReserveFund { get; set; }
    public decimal SyndicFeePercent { get; set; }
    public Guid? ParentCoOwnershipId { get; set; }
    public string? ParentCoOwnershipName { get; set; }
    public int ChildCount { get; set; }
    public int BuildingCount { get; set; }
    public int UnitCount { get; set; }
}

public class CreateCoOwnershipRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public CoOwnershipLevel Level { get; set; }
    public string? Description { get; set; }
    public string? RegulationReference { get; set; }
    public decimal AnnualBudget { get; set; }
    public decimal ReserveFund { get; set; }
    public decimal SyndicFeePercent { get; set; }
    public Guid? ParentCoOwnershipId { get; set; }
}

// === LeaseTenant ===
public class LeaseTenantDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string FullName => $"{FirstName} {LastName}";
    public string? CompanyName { get; set; }
    public string? TradeName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int ActiveLeaseCount { get; set; }
}

public class CreateLeaseTenantRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? CompanyName { get; set; }
    public string? TradeName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }
    public string? EmergencyContact { get; set; }
    public string? EmergencyPhone { get; set; }
}

// === Lease ===
public class LeaseDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;
    public Guid UnitId { get; set; }
    public string? UnitReference { get; set; }
    public Guid LeaseTenantId { get; set; }
    public string? TenantName { get; set; }
    public LeaseType Type { get; set; }
    public LeaseStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMonths { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal? Charges { get; set; }
    public decimal SecurityDeposit { get; set; }
    public DateTime? NextRevisionDate { get; set; }
    public decimal? TurnoverRentPercent { get; set; }
    public decimal? MarketingContribution { get; set; }
}

public class CreateLeaseRequest
{
    public Guid UnitId { get; set; }
    public Guid LeaseTenantId { get; set; }
    public string Reference { get; set; } = default!;
    public LeaseType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DurationMonths { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal? Charges { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal? AgencyFee { get; set; }
    public DateTime? NextRevisionDate { get; set; }
    public decimal? RevisionIndexPercent { get; set; }
    public decimal? TurnoverRentPercent { get; set; }
    public decimal? MarketingContribution { get; set; }
    public string? Notes { get; set; }
}

// === ChargeDefinition ===
public class ChargeDefinitionDto
{
    public Guid Id { get; set; }
    public Guid CoOwnershipId { get; set; }
    public string? CoOwnershipName { get; set; }
    public string Name { get; set; } = default!;
    public ChargeType Type { get; set; }
    public decimal AnnualAmount { get; set; }
    public string? DistributionKey { get; set; }
    public bool IsRecoverable { get; set; }
    public string? Description { get; set; }
    public int FiscalYear { get; set; }
}

public class CreateChargeDefinitionRequest
{
    public Guid CoOwnershipId { get; set; }
    public string Name { get; set; } = default!;
    public ChargeType Type { get; set; }
    public decimal AnnualAmount { get; set; }
    public string? DistributionKey { get; set; }
    public bool IsRecoverable { get; set; }
    public string? Description { get; set; }
    public int FiscalYear { get; set; }
}

// === ChargeAssignment ===
public class ChargeAssignmentDto
{
    public Guid Id { get; set; }
    public Guid ChargeDefinitionId { get; set; }
    public string? ChargeDefinitionName { get; set; }
    public Guid UnitId { get; set; }
    public string? UnitReference { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime DueDate { get; set; }
}

public class CreateChargeAssignmentRequest
{
    public Guid ChargeDefinitionId { get; set; }
    public Guid UnitId { get; set; }
    public int Year { get; set; }
    public int Quarter { get; set; }
    public decimal Amount { get; set; }
    public DateTime DueDate { get; set; }
}

// === Payment ===
public class PaymentDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "XOF";
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? Description { get; set; }
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public Guid? LeaseTenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? ChargeAssignmentId { get; set; }
}

public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? Description { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? LeaseTenantId { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? ChargeAssignmentId { get; set; }
}

// === Incident ===
public class IncidentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IncidentPriority Priority { get; set; }
    public IncidentStatus Status { get; set; }
    public string? Category { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitReference { get; set; }
    public Guid? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? ReportedByUserId { get; set; }
    public string? AssignedToUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int WorkOrderCount { get; set; }
}

public class CreateIncidentRequest
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IncidentPriority Priority { get; set; }
    public string? Category { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BuildingId { get; set; }
}

// === Supplier ===
public class SupplierDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Specialty { get; set; }
    public bool IsActive { get; set; }
    public int WorkOrderCount { get; set; }
}

public class CreateSupplierRequest
{
    public string Name { get; set; } = default!;
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxId { get; set; }
    public string? Specialty { get; set; }
    public string? BankDetails { get; set; }
}

// === WorkOrder ===
public class WorkOrderDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public WorkOrderStatus Status { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public Guid? IncidentId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public class CreateWorkOrderRequest
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal? EstimatedCost { get; set; }
    public DateTime? ScheduledDate { get; set; }
}

// === Meeting (AG) ===
public class MeetingDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string? CoOwnershipName { get; set; }
    public string Title { get; set; } = default!;
    public MeetingType Type { get; set; }
    public MeetingStatus Status { get; set; }
    public DateTime ScheduledDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public string? Location { get; set; }
    public string? ConvocationDocUrl { get; set; }
    public string? MinutesDocUrl { get; set; }
    public int? Quorum { get; set; }
    public int? AttendeesCount { get; set; }
    public string? Notes { get; set; }
    public int ResolutionCount { get; set; }
}

public class CreateMeetingRequest
{
    public Guid OrganizationId { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string Title { get; set; } = default!;
    public MeetingType Type { get; set; }
    public DateTime ScheduledDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}

// === Resolution ===
public class ResolutionDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public string? MeetingTitle { get; set; }
    public int OrderNumber { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ResolutionMajority RequiredMajority { get; set; }
    public int VotesFor { get; set; }
    public int VotesAgainst { get; set; }
    public int VotesAbstain { get; set; }
    public decimal SharesFor { get; set; }
    public decimal SharesAgainst { get; set; }
    public bool IsApproved { get; set; }
    public string? Notes { get; set; }
    public int VoteCount { get; set; }
}

public class CreateResolutionRequest
{
    public Guid MeetingId { get; set; }
    public int OrderNumber { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ResolutionMajority RequiredMajority { get; set; }
    public string? Notes { get; set; }
}

// === Vote ===
public class VoteDto
{
    public Guid Id { get; set; }
    public Guid ResolutionId { get; set; }
    public string? ResolutionTitle { get; set; }
    public Guid OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitReference { get; set; }
    public VoteResult Result { get; set; }
    public decimal ShareWeight { get; set; }
    public bool IsProxy { get; set; }
    public Guid? ProxyOwnerId { get; set; }
    public string? ProxyOwnerName { get; set; }
}

public class CreateVoteRequest
{
    public Guid ResolutionId { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? UnitId { get; set; }
    public VoteResult Result { get; set; }
    public decimal ShareWeight { get; set; }
    public bool IsProxy { get; set; }
    public Guid? ProxyOwnerId { get; set; }
}

// === Document (GED) ===
public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string FileName { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = default!;
    public DocumentCategory Category { get; set; }
    public string? Description { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? MeetingId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDocumentRequest
{
    public Guid OrganizationId { get; set; }
    public string FileName { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string ContentType { get; set; } = default!;
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = default!;
    public DocumentCategory Category { get; set; }
    public string? Description { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BuildingId { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? MeetingId { get; set; }
    public Guid? IncidentId { get; set; }
    public Guid? WorkOrderId { get; set; }
}

// === Notification ===
public class NotificationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationRequest
{
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? ActionUrl { get; set; }
}

// === AccountingEntry (SYSCOHADA) ===
public class AccountingEntryDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string EntryNumber { get; set; } = default!;
    public DateTime EntryDate { get; set; }
    public string JournalCode { get; set; } = default!;
    public string AccountCode { get; set; } = default!;
    public string? AccountLabel { get; set; }
    public string Description { get; set; } = default!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int FiscalYear { get; set; }
    public int Period { get; set; }
    public bool IsValidated { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ChargeAssignmentId { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string? CoOwnershipName { get; set; }
}

public class CreateAccountingEntryRequest
{
    public Guid OrganizationId { get; set; }
    public string EntryNumber { get; set; } = default!;
    public DateTime EntryDate { get; set; }
    public string JournalCode { get; set; } = default!;
    public string AccountCode { get; set; } = default!;
    public string? AccountLabel { get; set; }
    public string Description { get; set; } = default!;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public int FiscalYear { get; set; }
    public int Period { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ChargeAssignmentId { get; set; }
    public Guid? LeaseId { get; set; }
    public Guid? CoOwnershipId { get; set; }
}
