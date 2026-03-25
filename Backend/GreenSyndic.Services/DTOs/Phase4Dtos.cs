using GreenSyndic.Core.Enums;

namespace GreenSyndic.Services.DTOs;

// ═══════════════════════════════════════════════════════════
//  Phase 4A — Appels de loyer
// ═══════════════════════════════════════════════════════════

public class RentCallDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LeaseId { get; set; }
    public string? LeaseReference { get; set; }
    public string? TenantName { get; set; }
    public string? UnitReference { get; set; }
    public string Reference { get; set; } = default!;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ChargesAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public RentCallStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal? TurnoverRentAmount { get; set; }
    public decimal? MarketingContribution { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRentCallRequest
{
    public Guid OrganizationId { get; set; }
    public Guid LeaseId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime DueDate { get; set; }
    public decimal? TurnoverRentAmount { get; set; }
    public decimal? MarketingContribution { get; set; }
    public string? Notes { get; set; }
}

public class GenerateRentCallsRequest
{
    public Guid OrganizationId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime DueDate { get; set; }
    public Guid? CoOwnershipId { get; set; }   // Optional filter
}

public class GenerateRentCallsResultDto
{
    public int GeneratedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> SkippedReasons { get; set; } = [];
    public List<RentCallDto> GeneratedCalls { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════
//  Phase 4A — Quittances
// ═══════════════════════════════════════════════════════════

public class RentReceiptDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RentCallId { get; set; }
    public Guid LeaseId { get; set; }
    public string? LeaseReference { get; set; }
    public string? TenantName { get; set; }
    public string Reference { get; set; } = default!;
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ChargesAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public RentReceiptStatus Status { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public Guid? PaymentId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRentReceiptRequest
{
    public Guid OrganizationId { get; set; }
    public Guid RentCallId { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal RentAmount { get; set; }
    public decimal ChargesAmount { get; set; }
    public string? Notes { get; set; }
}

// ═══════════════════════════════════════════════════════════
//  Phase 4A — Révisions de loyer
// ═══════════════════════════════════════════════════════════

public class LeaseRevisionDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LeaseId { get; set; }
    public string? LeaseReference { get; set; }
    public string? TenantName { get; set; }
    public string Reference { get; set; } = default!;
    public RevisionType Type { get; set; }
    public RevisionStatus Status { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime? NotificationDate { get; set; }
    public decimal PreviousRent { get; set; }
    public decimal NewRent { get; set; }
    public decimal VariationPercent { get; set; }
    public string? IndexName { get; set; }
    public decimal? IndexValueOld { get; set; }
    public decimal? IndexValueNew { get; set; }
    public string? LegalBasis { get; set; }
    public string? Justification { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ContestedAt { get; set; }
    public string? ContestationReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateLeaseRevisionRequest
{
    public Guid OrganizationId { get; set; }
    public Guid LeaseId { get; set; }
    public RevisionType Type { get; set; }
    public DateTime EffectiveDate { get; set; }
    public decimal NewRent { get; set; }
    public string? IndexName { get; set; }
    public decimal? IndexValueOld { get; set; }
    public decimal? IndexValueNew { get; set; }
    public string? LegalBasis { get; set; }
    public string? Justification { get; set; }
    public string? Notes { get; set; }
}

public class RespondRevisionRequest
{
    public bool Accepted { get; set; }
    public string? ContestationReason { get; set; }
}

// ═══════════════════════════════════════════════════════════
//  Phase 4A — Régularisation des charges
// ═══════════════════════════════════════════════════════════

public class ChargeRegularizationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LeaseId { get; set; }
    public string? LeaseReference { get; set; }
    public string? TenantName { get; set; }
    public string Reference { get; set; } = default!;
    public RegularizationType Type { get; set; }
    public RegularizationStatus Status { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalProvisioned { get; set; }
    public decimal TotalActual { get; set; }
    public decimal Balance { get; set; }
    public string? BreakdownJson { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? ContestedAt { get; set; }
    public string? ContestationReason { get; set; }
    public DateTime? SettledAt { get; set; }
    public Guid? PaymentId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateChargeRegularizationRequest
{
    public Guid OrganizationId { get; set; }
    public Guid LeaseId { get; set; }
    public RegularizationType Type { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalProvisioned { get; set; }
    public decimal TotalActual { get; set; }
    public string? BreakdownJson { get; set; }
    public string? Notes { get; set; }
}

public class RespondRegularizationRequest
{
    public bool Accepted { get; set; }
    public string? ContestationReason { get; set; }
}

// ═══════════════════════════════════════════════════════════
//  Phase 4A — Candidatures locataires
// ═══════════════════════════════════════════════════════════

public class TenantApplicationDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UnitId { get; set; }
    public string? UnitReference { get; set; }
    public string Reference { get; set; } = default!;
    public ApplicationStatus Status { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? CompanyName { get; set; }
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public string? EmployerName { get; set; }
    public string? EmploymentType { get; set; }
    public int? EmploymentDurationMonths { get; set; }
    public string? GuarantorName { get; set; }
    public string? GuarantorPhone { get; set; }
    public string? GuarantorRelation { get; set; }
    public int? Score { get; set; }
    public ApplicationScoreLevel? ScoreLevel { get; set; }
    public string? ScoreDetailsJson { get; set; }
    public decimal DesiredRent { get; set; }
    public DateTime? DesiredMoveInDate { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? LeaseId { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTenantApplicationRequest
{
    public Guid OrganizationId { get; set; }
    public Guid UnitId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? CompanyName { get; set; }
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }
    public decimal? MonthlyIncome { get; set; }
    public string? EmployerName { get; set; }
    public string? EmploymentType { get; set; }
    public int? EmploymentDurationMonths { get; set; }
    public string? GuarantorName { get; set; }
    public string? GuarantorPhone { get; set; }
    public string? GuarantorRelation { get; set; }
    public decimal DesiredRent { get; set; }
    public DateTime? DesiredMoveInDate { get; set; }
    public string? Notes { get; set; }
}

public class ReviewApplicationRequest
{
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
}

public class ApplicationScoreDto
{
    public int TotalScore { get; set; }
    public ApplicationScoreLevel Level { get; set; }
    public decimal IncomeToRentRatio { get; set; }
    public int IncomeScore { get; set; }
    public int EmploymentScore { get; set; }
    public int GuarantorScore { get; set; }
    public int DocumentsScore { get; set; }
}

// ═══════════════════════════════════════════════════════════
//  Phase 4C — Enrichissement Lease
// ═══════════════════════════════════════════════════════════

public class LeaseRenewRequest
{
    public int? NewDurationMonths { get; set; }
    public decimal? NewMonthlyRent { get; set; }
    public DateTime? NewStartDate { get; set; }
    public string? Notes { get; set; }
}

public class LeaseTerminateRequest
{
    public DateTime TerminationDate { get; set; }
    public string? Reason { get; set; }
}

public class LandlordStatementDto
{
    public Guid LeaseId { get; set; }
    public string LeaseReference { get; set; } = default!;
    public string TenantName { get; set; } = default!;
    public string UnitReference { get; set; } = default!;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TotalRentDue { get; set; }
    public decimal TotalRentReceived { get; set; }
    public decimal TotalChargesProvisioned { get; set; }
    public decimal TotalChargesActual { get; set; }
    public decimal ChargesBalance { get; set; }
    public decimal NetResult { get; set; }
    public List<LandlordStatementLineDto> RentLines { get; set; } = [];
    public List<LandlordStatementLineDto> ChargeLines { get; set; } = [];
}

public class LandlordStatementLineDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = default!;
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
}

// ═══════════════════════════════════════════════════════════
//  Phase 4C — Enrichissement Payment
// ═══════════════════════════════════════════════════════════

public class ReconcilePaymentRequest
{
    public Guid RentCallId { get; set; }
}
