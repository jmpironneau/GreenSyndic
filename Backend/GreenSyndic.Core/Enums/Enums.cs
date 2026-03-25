namespace GreenSyndic.Core.Enums;

public enum PropertyType
{
    VillaDuplex,
    Apartment,
    CommercialUnit,
    RetailUnit,
    Office,
    Cinema,
    Restaurant,
    FastFood,
    FoodCourt,
    GasStation,
    Clinic,
    Bank,
    School,
    SportsClub,
    ClubHouse,
    GuardHouse,
    WaterTreatment, // STEP
    Parking,
    Storage
}

public enum UnitStatus
{
    Available,
    Occupied,
    Vacant,
    UnderRenovation,
    Reserved
}

public enum LeaseType
{
    Residential,        // CCH art. 414-450
    Commercial,         // OHADA AUDCG art. 101-134
    Professional,
    Concession,         // Station Oryx
    HealthFacility,     // Clinique Novamed
    BankingFacility,    // Afrika Banque
    Educational,        // École
    SportsLeisure
}

public enum LeaseStatus
{
    Draft,
    Active,
    Expired,
    Terminated,
    Renewed,
    InDispute
}

public enum ChargeType
{
    HorizontalCopropriete,  // Copropriété horizontale (Green City)
    VerticalCopropriete,    // Copropriété verticale (par immeuble)
    Water,
    Electricity,
    CommonAreaMaintenance,
    Security,
    GreenSpaces,
    Elevator,
    Generator,
    WaterTreatment,         // STEP
    PublicLighting,
    Insurance,
    ManagementFee,
    SpecialWorks,
    SinkingFund             // Fonds de réserve
}

public enum PaymentMethod
{
    BankTransfer,
    OrangeMoney,
    MtnMoney,
    Wave,
    CinetPay,
    PayDunya,
    Cash,
    Check
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Cancelled
}

public enum IncidentStatus
{
    Reported,
    Acknowledged,
    InProgress,
    Resolved,
    Closed,
    Rejected
}

public enum IncidentPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum MeetingType
{
    OrdinaryGeneral,    // AG ordinaire
    ExtraordinaryGeneral, // AG extraordinaire
    CouncilMeeting,     // Conseil syndical
    BoardMeeting
}

public enum MeetingStatus
{
    Planned,
    ConvocationSent,
    InProgress,
    Completed,
    Cancelled
}

public enum VoteResult
{
    For,
    Against,
    Abstain,
    Absent
}

public enum ResolutionMajority
{
    Simple,             // Art. 24 - majorité simple
    Absolute,           // Art. 25 - majorité absolue
    DoubleMajority,     // Art. 26 - double majorité
    Unanimity           // Unanimité
}

public enum DocumentCategory
{
    Lease,
    Invoice,
    Receipt,
    Meeting,
    LegalNotice,
    Insurance,
    WorkOrder,
    Plan,
    Regulation,
    Financial,
    Other
}

public enum WorkOrderStatus
{
    Draft,
    Approved,
    InProgress,
    Completed,
    Invoiced,
    Paid,
    Cancelled
}

public enum UserRole
{
    SuperAdmin,
    SyndicManager,          // Syndic / Gestionnaire
    SyndicAccountant,       // Comptable syndic
    SyndicTechnician,       // Technicien
    CouncilPresident,       // Président du conseil syndical
    CouncilMember,          // Membre du conseil syndical
    Owner,                  // Copropriétaire
    Tenant,                 // Locataire
    CommercialTenant,       // Locataire commercial
    Supplier,               // Prestataire/Fournisseur
    SecurityAgent,          // Agent de sécurité
    ReadOnly                // Consultation seule
}

public enum CoOwnershipLevel
{
    Horizontal, // Copropriété horizontale (ensemble Green City)
    Vertical    // Copropriété verticale (par immeuble)
}

// ── Phase 3A : Assemblées Générales ──

public enum AttendanceStatus
{
    Expected,           // Convoqué, pas encore répondu
    Confirmed,          // Présence confirmée
    PresentInPerson,    // Présent physiquement
    PresentRemote,      // Présent en visioconférence
    RepresentedByProxy, // Représenté par procuration
    Absent              // Absent non représenté
}

public enum ConvocationMethod
{
    Email,
    RegisteredMail,     // Lettre recommandée (art. 388 CCH)
    ElectronicSignature,// Voie électronique avec accusé
    HandDelivery,       // Remise en main propre
    Sms,
    Push
}

public enum AgendaItemType
{
    Information,        // Point d'information (pas de vote)
    Resolution,         // Point donnant lieu à un vote
    Election,           // Élection (bureau, conseil syndical)
    Questions           // Questions diverses
}

// ── Phase 3B : Communication ──

public enum MessageChannel
{
    Email,
    Sms,
    Push,
    PostalMail,         // Courrier postal
    RegisteredMail      // LRAR
}

public enum MessageStatus
{
    Draft,
    Scheduled,
    Sending,
    Sent,
    Failed,
    Cancelled
}

public enum BroadcastStatus
{
    Draft,
    Scheduled,
    Sending,
    Completed,
    PartiallyCompleted,
    Cancelled
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Bounced,
    Failed,
    Returned            // Courrier retourné (NPAI)
}

// ── Phase 4 : Gestion Locative ──

public enum RentCallStatus
{
    Draft,
    Sent,
    PartiallyPaid,
    Paid,
    Overdue,
    Cancelled
}

public enum RentReceiptStatus
{
    Draft,
    Issued,
    Sent,
    Cancelled
}

public enum RevisionType
{
    Triennale,          // Révision triennale (CCH art. 423-424 / AUDCG art. 116)
    Indexation,         // Indexation annuelle sur indice
    Contractual,        // Clause contractuelle spécifique
    MarketValue         // Revalorisation au prix du marché (renouvellement)
}

public enum RevisionStatus
{
    Pending,
    Notified,
    Accepted,
    Contested,
    Applied,
    Cancelled
}

public enum RegularizationType
{
    Annual,             // Régularisation annuelle des charges
    Interim,            // Régularisation intermédiaire
    Final               // Régularisation de sortie
}

public enum RegularizationStatus
{
    Draft,
    Calculated,
    Notified,
    Accepted,
    Contested,
    Settled,
    Cancelled
}

public enum ApplicationStatus
{
    Submitted,
    UnderReview,
    DocumentsPending,
    Approved,
    Rejected,
    Withdrawn,
    LeaseCreated
}

public enum ApplicationScoreLevel
{
    Excellent,
    Good,
    Average,
    Poor,
    Insufficient
}

// ── Phase 6 : Modules secondaires ──

public enum ExportFormat
{
    Csv,
    Excel,
    Pdf,
    Json
}

public enum IncidentCategory
{
    Plumbing,           // Plomberie
    Electrical,         // Électricité
    Locksmith,          // Serrurerie
    Elevator,           // Ascenseur
    CommonAreas,        // Parties communes
    GreenSpaces,        // Espaces verts
    Security,           // Sécurité
    WaterTreatment,     // STEP
    AirConditioning,    // Climatisation
    Structural,         // Structure / gros œuvre
    Cleaning,           // Nettoyage
    Pest,               // Nuisibles
    Noise,              // Nuisances sonores
    Other               // Autre
}

public enum LegalDomain
{
    Copropriete,        // Loi copropriété CI / CCH
    BailResidentiel,    // Bail résidentiel CCH art. 414-450
    BailCommercial,     // OHADA AUDCG art. 101-134
    Fiscalite,          // Fiscalité immobilière
    Urbanisme,          // Urbanisme / permis
    Assurance,          // Assurance immeuble
    Travail,            // Droit du travail (gardiens, etc.)
    Environnement       // Environnement / STEP
}
