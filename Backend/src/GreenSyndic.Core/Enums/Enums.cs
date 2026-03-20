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

public enum CoproprieteLevelType
{
    Horizontal, // Copropriété horizontale (ensemble Green City)
    Vertical    // Copropriété verticale (par immeuble)
}
