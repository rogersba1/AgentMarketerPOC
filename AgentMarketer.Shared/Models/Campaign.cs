using System.ComponentModel.DataAnnotations;

namespace AgentMarketer.Shared.Models;

/// <summary>
/// Represents a marketing campaign
/// </summary>
public class Campaign
{
    /// <summary>
    /// Unique identifier for the campaign
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the campaign
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Campaign objective or goal
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Goal { get; set; } = string.Empty;

    /// <summary>
    /// Target audience description
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Campaign status
    /// </summary>
    public CampaignStatus Status { get; set; } = CampaignStatus.Created;

    /// <summary>
    /// When the campaign was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the campaign was executed (if applicable)
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// User who created the campaign (for web interface)
    /// </summary>
    public string CreatedBy { get; set; } = "system";

    /// <summary>
    /// Target company IDs for the campaign
    /// </summary>
    public List<string> CompanyIds { get; set; } = [];

    /// <summary>
    /// Campaign component types (landing page, email, etc.)
    /// </summary>
    public List<string> Components { get; set; } = [];

    /// <summary>
    /// Additional metadata for the campaign
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Campaign execution status
/// </summary>
public enum CampaignStatus
{
    /// <summary>
    /// Campaign has been created but not planned
    /// </summary>
    Created,

    /// <summary>
    /// Campaign plan has been generated
    /// </summary>
    Planned,

    /// <summary>
    /// Campaign is currently executing
    /// </summary>
    Executing,

    /// <summary>
    /// Campaign execution is paused (waiting for approvals)
    /// </summary>
    Paused,

    /// <summary>
    /// Campaign has completed successfully
    /// </summary>
    Completed,

    /// <summary>
    /// Campaign execution failed
    /// </summary>
    Failed,

    /// <summary>
    /// Campaign was cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Status of approval requests for company briefs or content
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Approval request is pending review
    /// </summary>
    Pending,

    /// <summary>
    /// Request has been approved
    /// </summary>
    Approved,

    /// <summary>
    /// Request has been rejected
    /// </summary>
    Rejected,

    /// <summary>
    /// Request has been submitted for further review
    /// </summary>
    UnderReview
}
