using GreenSyndic.Core.Enums;

namespace GreenSyndic.Core.Entities;

/// <summary>
/// Legal reference / veille juridique — articles de loi, jurisprudence, réglementation.
/// Shared across all organizations (system-level knowledge base).
/// </summary>
public class LegalReference : BaseEntity
{
    public string Code { get; set; } = default!;             // "CCH-414", "AUDCG-101"
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;          // Full text of the article
    public LegalDomain Domain { get; set; }
    public string? Source { get; set; }                      // "CCH", "AUDCG", "Décret 2019-XXX"
    public string? Url { get; set; }                         // Link to official text
    public DateTime? EffectiveDate { get; set; }             // Date d'entrée en vigueur
    public bool IsActive { get; set; } = true;
    public string? Tags { get; set; }                        // JSON array: ["loyer", "révision", "résiliation"]
    public string? Notes { get; set; }
}
