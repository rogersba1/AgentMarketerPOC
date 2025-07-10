using System.ComponentModel.DataAnnotations;
using AgentMarketer.Shared.Models;

namespace AgentMarketer.Shared.DTOs;

/// <summary>
/// Response containing company brief details
/// </summary>
public record CompanyBriefResponse
{
    public string CompanyId { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string Brief { get; init; } = string.Empty;
    public string Industry { get; init; } = string.Empty;
    public string CampaignId { get; init; } = string.Empty;
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? HumanFeedback { get; set; }
    public bool IsModified { get; init; }
    public string Content { get; init; } = string.Empty;
    public List<string> KeyMessages { get; init; } = [];
    public string TargetAudience { get; init; } = string.Empty;
    public decimal EstimatedBudget { get; init; }
    public int ProjectedReach { get; init; }
    public string? ApproverFeedback { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
}

/// <summary>
/// Request to approve or reject a company brief
/// </summary>
public record ApprovalRequest
{
    [StringLength(2000)]
    public string Feedback { get; init; } = string.Empty;

    public bool IsApproved { get; init; }

    [StringLength(10000)]
    public string? ModifiedBrief { get; init; }

    public ApprovalStatus Action { get; init; } = ApprovalStatus.Pending;
    
    public string? ApprovedBy { get; init; }

    /// <summary>
    /// Whether the brief content was modified
    /// </summary>
    public bool IsModified => !string.IsNullOrEmpty(ModifiedBrief);
}

/// <summary>
/// Response from approval action
/// </summary>
public record ApprovalActionResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string UpdatedStatus { get; init; } = string.Empty;
    public string ApprovalId { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
    public CompanyBriefResponse? UpdatedBrief { get; init; }
}

/// <summary>
/// Specific approval response type 
/// </summary>
public record ApprovalResponse : ApprovalActionResponse
{
    public ApprovalResponse()
    {
    }

    public ApprovalResponse(bool success, string message, string updatedStatus)
    {
        Success = success;
        Message = message;
        UpdatedStatus = updatedStatus;
    }
}

/// <summary>
/// Summary of pending approvals for a campaign
/// </summary>
public record PendingApprovalsSummary
{
    public string CampaignId { get; init; } = string.Empty;
    public string CampaignName { get; init; } = string.Empty;
    public int PendingCount { get; init; }
    public int TotalCount { get; init; }
    public DateTime? OldestPending { get; init; }
    public DateTime? OldestPendingDate { get; init; }
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Individual approval item for bulk operations
/// </summary>
public record BulkApprovalItem
{
    public string CompanyId { get; init; } = string.Empty;
    public ApprovalStatus Action { get; init; } = ApprovalStatus.Pending;
    public string? Feedback { get; init; }
}

/// <summary>
/// Request for bulk approval actions
/// </summary>
public record BulkApprovalRequest
{
    [Required]
    public string CampaignId { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<BulkApprovalItem> Approvals { get; init; } = [];

    [Required]
    [AllowedValues("approve", "reject")]
    public string Action { get; init; } = string.Empty;

    [StringLength(2000)]
    public string Feedback { get; init; } = string.Empty;
    
    public string? ApprovedBy { get; init; }
}

/// <summary>
/// Response from bulk approval action
/// </summary>
public record BulkApprovalResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int ProcessedCount { get; init; }
    public List<string> FailedCompanies { get; init; } = [];
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public int TotalCount { get; init; }
    public List<ApprovalActionResponse> Results { get; init; } = [];
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
