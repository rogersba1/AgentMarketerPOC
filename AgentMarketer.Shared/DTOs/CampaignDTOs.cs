using System.ComponentModel.DataAnnotations;

namespace AgentMarketer.Shared.DTOs;

/// <summary>
/// Request to create a new marketing campaign
/// </summary>
/// <param name="Name">Display name for the campaign</param>
/// <param name="Goal">Campaign objective or goal</param>
/// <param name="Audience">Target audience description</param>
/// <param name="Components">Campaign component types (landing page, email, etc.)</param>
public record CreateCampaignRequest(
    [Required]
    [StringLength(200, MinimumLength = 3)]
    string Name,

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    string Goal,

    [Required]
    [StringLength(1000, MinimumLength = 10)]
    string Audience,

    [Required]
    [MinLength(1)]
    List<string> Components
);

/// <summary>
/// Response containing campaign details
/// </summary>
/// <param name="Id">Unique identifier for the campaign</param>
/// <param name="Name">Display name for the campaign</param>
/// <param name="Goal">Campaign objective or goal</param>
/// <param name="Audience">Target audience description</param>
/// <param name="Components">Campaign component types</param>
/// <param name="Status">Current campaign status</param>
/// <param name="CreatedAt">When the campaign was created</param>
/// <param name="ExecutedAt">When the campaign was executed (if applicable)</param>
/// <param name="CreatedBy">User who created the campaign</param>
/// <param name="CompanyCount">Number of target companies</param>
/// <param name="ApprovedBriefs">Number of approved company briefs</param>
/// <param name="PendingApprovals">Number of briefs awaiting approval</param>
public record CampaignResponse(
    string Id,
    string Name,
    string Goal,
    string Audience,
    List<string> Components,
    string Status,
    DateTime CreatedAt,
    DateTime? ExecutedAt,
    string CreatedBy,
    int CompanyCount,
    int ApprovedBriefs,
    int PendingApprovals
);

/// <summary>
/// Summary response for campaign lists
/// </summary>
/// <param name="Id">Unique identifier for the campaign</param>
/// <param name="Name">Display name for the campaign</param>
/// <param name="Goal">Campaign objective or goal</param>
/// <param name="Status">Current campaign status</param>
/// <param name="CreatedAt">When the campaign was created</param>
/// <param name="CompanyCount">Number of target companies</param>
/// <param name="PendingApprovals">Number of briefs awaiting approval</param>
public record CampaignSummaryResponse(
    string Id,
    string Name,
    string Goal,
    string Status,
    DateTime CreatedAt,
    int CompanyCount,
    int PendingApprovals
);

/// <summary>
/// Request to create a campaign plan
/// </summary>
/// <param name="CompanyNames">Optional list of specific company names to target</param>
public record CreatePlanRequest(
    List<string>? CompanyNames = null
);

/// <summary>
/// Response containing plan creation result
/// </summary>
/// <param name="Success">Whether plan creation was successful</param>
/// <param name="Message">Result message or error details</param>
/// <param name="PlanStepCount">Number of steps in the generated plan</param>
public record CreatePlanResponse(
    bool Success,
    string Message,
    int PlanStepCount
);

/// <summary>
/// Response containing campaign execution status
/// </summary>
/// <param name="Status">Current execution status</param>
/// <param name="Message">Status message or error details</param>
/// <param name="Progress">Execution progress (0-100)</param>
/// <param name="CompletedSteps">Number of completed steps</param>
/// <param name="TotalSteps">Total number of steps</param>
/// <param name="CurrentStep">Description of current step (if executing)</param>
/// <param name="PendingApprovals">Number of briefs awaiting approval</param>
public record ExecutionStatusResponse(
    string Status,
    string Message,
    int Progress,
    int CompletedSteps,
    int TotalSteps,
    string? CurrentStep,
    int PendingApprovals
);
