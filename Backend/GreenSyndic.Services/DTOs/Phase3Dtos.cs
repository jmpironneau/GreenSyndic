using GreenSyndic.Core.Enums;

namespace GreenSyndic.Services.DTOs;

// ═══════════════════════════════════════════════════════════
//  Phase 3A — Assemblées Générales
// ═══════════════════════════════════════════════════════════

public class MeetingAttendeeDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public Guid OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public AttendanceStatus Status { get; set; }
    public decimal SharesRepresented { get; set; }
    public Guid? ProxyHolderId { get; set; }
    public string? ProxyHolderName { get; set; }
    public ConvocationMethod? ConvocationMethod { get; set; }
    public DateTime? ConvocationSentAt { get; set; }
    public DateTime? ConvocationReceivedAt { get; set; }
    public bool HasSigned { get; set; }
    public DateTime? SignedAt { get; set; }
}

public class CreateMeetingAttendeeRequest
{
    public Guid MeetingId { get; set; }
    public Guid OwnerId { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Expected;
    public decimal SharesRepresented { get; set; }
    public Guid? ProxyHolderId { get; set; }
    public ConvocationMethod? ConvocationMethod { get; set; }
}

public class UpdateAttendanceStatusRequest
{
    public AttendanceStatus Status { get; set; }
    public Guid? ProxyHolderId { get; set; }
    public bool HasSigned { get; set; }
}

public class MeetingAgendaItemDto
{
    public Guid Id { get; set; }
    public Guid MeetingId { get; set; }
    public int OrderNumber { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public AgendaItemType Type { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public Guid? ResolutionId { get; set; }
    public string? AttachmentUrls { get; set; }
}

public class CreateMeetingAgendaItemRequest
{
    public Guid MeetingId { get; set; }
    public int OrderNumber { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public AgendaItemType Type { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public Guid? ResolutionId { get; set; }
    public string? AttachmentUrls { get; set; }
}

public class ResolutionTemplateDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ResolutionMajority DefaultMajority { get; set; }
    public string? Category { get; set; }
    public string? LegalReference { get; set; }
    public string? TemplateText { get; set; }
    public bool IsActive { get; set; }
}

public class CreateResolutionTemplateRequest
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public ResolutionMajority DefaultMajority { get; set; }
    public string? Category { get; set; }
    public string? LegalReference { get; set; }
    public string? TemplateText { get; set; }
}

/// <summary>Résultat du calcul de quorum pour une AG.</summary>
public class QuorumResultDto
{
    public Guid MeetingId { get; set; }
    public int TotalOwners { get; set; }
    public int PresentOrRepresented { get; set; }
    public decimal TotalShares { get; set; }
    public decimal RepresentedShares { get; set; }
    public decimal QuorumPercentage { get; set; }
    public bool HasQuorum { get; set; }
}

/// <summary>Demande de génération de convocations.</summary>
public class SendConvocationsRequest
{
    public ConvocationMethod Method { get; set; }
    public string? CustomSubject { get; set; }
    public string? CustomBody { get; set; }
}

// ═══════════════════════════════════════════════════════════
//  Phase 3B — Communication
// ═══════════════════════════════════════════════════════════

public class CommunicationMessageDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public MessageChannel Channel { get; set; }
    public MessageStatus Status { get; set; }
    public string? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientName { get; set; }
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public Guid? TemplateId { get; set; }
    public Guid? BroadcastId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int DeliveryLogCount { get; set; }
}

public class CreateMessageRequest
{
    public Guid OrganizationId { get; set; }
    public MessageChannel Channel { get; set; }
    public string? RecipientUserId { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientAddress { get; set; }
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public Guid? TemplateId { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class MessageTemplateDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public MessageChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = default!;
    public string? Category { get; set; }
    public string? AvailableVariables { get; set; }
    public bool IsActive { get; set; }
}

public class CreateMessageTemplateRequest
{
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public MessageChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = default!;
    public string? Category { get; set; }
    public string? AvailableVariables { get; set; }
}

public class BroadcastDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public MessageChannel Channel { get; set; }
    public BroadcastStatus Status { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Subject { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string? TargetRole { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }
}

public class CreateBroadcastRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public MessageChannel Channel { get; set; }
    public Guid? TemplateId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public Guid? CoOwnershipId { get; set; }
    public string? TargetRole { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class BroadcastRecipientDto
{
    public Guid Id { get; set; }
    public Guid BroadcastId { get; set; }
    public string? UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? MessageId { get; set; }
    public MessageStatus? MessageStatus { get; set; }
}

public class AddBroadcastRecipientRequest
{
    public string? UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? MergeData { get; set; }
}

public class MessageDeliveryLogDto
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public DeliveryStatus Status { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Details { get; set; }
}
