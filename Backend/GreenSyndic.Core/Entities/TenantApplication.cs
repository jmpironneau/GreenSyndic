using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Candidature locataire avec scoring automatique.
/// Gère le processus de sélection des locataires.
/// </summary>
public class TenantApplication : BaseEntity
{
    public Guid OrganizationId { get; set; }

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = default!;

    public string Reference { get; set; } = default!;         // CAND-2026-001

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;

    // Applicant info
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? CompanyName { get; set; }                    // For commercial applications
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string? NationalId { get; set; }
    public string? TaxId { get; set; }                          // RCCM for commercial

    // Financial info for scoring
    public decimal? MonthlyIncome { get; set; }                 // Revenus mensuels
    public string? EmployerName { get; set; }
    public string? EmploymentType { get; set; }                 // CDI, CDD, Indépendant
    public int? EmploymentDurationMonths { get; set; }

    // Guarantor
    public string? GuarantorName { get; set; }
    public string? GuarantorPhone { get; set; }
    public string? GuarantorRelation { get; set; }

    // Scoring
    public int? Score { get; set; }                             // 0-100
    public ApplicationScoreLevel? ScoreLevel { get; set; }
    public string? ScoreDetailsJson { get; set; }               // Détail du scoring

    public decimal DesiredRent { get; set; }                    // Budget loyer souhaité
    public DateTime? DesiredMoveInDate { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }

    // Link to created lease
    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }

    public string? Notes { get; set; }
}
