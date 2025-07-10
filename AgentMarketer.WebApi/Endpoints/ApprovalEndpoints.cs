using AgentMarketer.Shared.Contracts;
using AgentMarketer.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AgentMarketer.WebApi.Endpoints;

/// <summary>
/// API endpoints for managing company brief approvals
/// </summary>
public static class ApprovalEndpoints
{
    /// <summary>
    /// Configures approval-related API endpoints
    /// </summary>
    /// <param name="app">The web application</param>
    /// <returns>The configured web application</returns>
    public static WebApplication MapApprovalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/approvals")
            .WithTags("Approvals")
            .WithOpenApi();

        // Get all company briefs for a campaign
        group.MapGet("/campaigns/{campaignId}/briefs", GetCompanyBriefsAsync)
            .WithName("GetCompanyBriefs")
            .WithSummary("Get all company briefs for a campaign")
            .WithDescription("Retrieves all company briefs (approved, pending, and rejected) for the specified campaign")
            .Produces<List<CompanyBriefResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // Get pending briefs for a campaign
        group.MapGet("/campaigns/{campaignId}/briefs/pending", GetPendingBriefsAsync)
            .WithName("GetPendingBriefs")
            .WithSummary("Get pending company briefs for a campaign")
            .WithDescription("Retrieves all company briefs that are pending approval for the specified campaign")
            .Produces<List<CompanyBriefResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // Get specific company brief
        group.MapGet("/campaigns/{campaignId}/briefs/{companyId}", GetCompanyBriefAsync)
            .WithName("GetCompanyBrief")
            .WithSummary("Get a specific company brief")
            .WithDescription("Retrieves the company brief for a specific company within a campaign")
            .Produces<CompanyBriefResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // Process approval for a company brief
        group.MapPost("/campaigns/{campaignId}/briefs/{companyId}/approve", ProcessApprovalAsync)
            .WithName("ProcessApproval")
            .WithSummary("Approve or reject a company brief")
            .WithDescription("Processes an approval decision (approve/reject) for a specific company brief")
            .Accepts<ApprovalRequest>("application/json")
            .Produces<ApprovalActionResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // Process bulk approvals
        group.MapPost("/campaigns/{campaignId}/briefs/bulk-approve", ProcessBulkApprovalAsync)
            .WithName("ProcessBulkApproval")
            .WithSummary("Process multiple approval decisions")
            .WithDescription("Processes multiple approval decisions for company briefs in a single operation")
            .Accepts<BulkApprovalRequest>("application/json")
            .Produces<BulkApprovalResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // Get pending approvals summary across all campaigns
        group.MapGet("/pending-summary", GetPendingApprovalsSummaryAsync)
            .WithName("GetPendingApprovalsSummary")
            .WithSummary("Get pending approvals summary")
            .WithDescription("Retrieves a summary of pending approvals across all campaigns")
            .Produces<List<PendingApprovalsSummary>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // Get pending approvals summary for a specific campaign
        group.MapGet("/campaigns/{campaignId}/pending-summary", GetCampaignPendingApprovalsAsync)
            .WithName("GetCampaignPendingApprovals")
            .WithSummary("Get pending approvals for a campaign")
            .WithDescription("Retrieves a summary of pending approvals for the specified campaign")
            .Produces<PendingApprovalsSummary>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return app;
    }

    /// <summary>
    /// Get all company briefs for a campaign
    /// </summary>
    private static async Task<IResult> GetCompanyBriefsAsync(
        [FromRoute] string campaignId,
        [FromServices] IApprovalService approvalService,
        [FromServices] ILogger<IApprovalService> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(campaignId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Campaign ID",
                    Detail = "Campaign ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var briefs = await approvalService.GetCompanyBriefsAsync(campaignId, cancellationToken);
            return Results.Ok(briefs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting company briefs for campaign: {CampaignId}", campaignId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while retrieving company briefs",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get pending company briefs for a campaign
    /// </summary>
    private static async Task<IResult> GetPendingBriefsAsync(
        [FromRoute] string campaignId,
        [FromServices] IApprovalService approvalService,
        [FromServices] ILogger<IApprovalService> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(campaignId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Campaign ID",
                    Detail = "Campaign ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var briefs = await approvalService.GetPendingBriefsAsync(campaignId, cancellationToken);
            return Results.Ok(briefs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting pending briefs for campaign: {CampaignId}", campaignId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while retrieving pending briefs",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get a specific company brief
    /// </summary>
    private static async Task<IResult> GetCompanyBriefAsync(
        [FromRoute] string campaignId,
        [FromRoute] string companyId,
        [FromServices] IApprovalService approvalService,
        [FromServices] ILogger<IApprovalService> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(campaignId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Campaign ID",
                    Detail = "Campaign ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(companyId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Company ID",
                    Detail = "Company ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var brief = await approvalService.GetCompanyBriefAsync(campaignId, companyId, cancellationToken);
            
            if (brief == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Company Brief Not Found",
                    Detail = $"No company brief found for campaign '{campaignId}' and company '{companyId}'",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Results.Ok(brief);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting company brief for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while retrieving the company brief",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Process approval for a company brief
    /// </summary>
    private static async Task<IResult> ProcessApprovalAsync(
        [FromRoute] string campaignId,
        [FromRoute] string companyId,
        [FromBody] ApprovalRequest request,
        [FromServices] IApprovalService approvalService,
        [FromServices] ILogger<IApprovalService> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(campaignId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Campaign ID",
                    Detail = "Campaign ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            if (string.IsNullOrWhiteSpace(companyId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Company ID",
                    Detail = "Company ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Validate the request
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage).ToArray();
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Validation Failed",
                    Detail = string.Join("; ", errors),
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var result = await approvalService.ProcessApprovalAsync(campaignId, companyId, request, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing approval for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while processing the approval",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Process bulk approval for multiple company briefs
    /// </summary>
    private static async Task<IResult> ProcessBulkApprovalAsync(
        [FromRoute] string campaignId,
        [FromBody] BulkApprovalRequest request,
        [FromServices] IApprovalService approvalService,
        [FromServices] ILogger<IApprovalService> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(campaignId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Campaign ID",
                    Detail = "Campaign ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Ensure the campaign ID in the route matches the request
            if (!string.Equals(campaignId, request.CampaignId, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Campaign ID Mismatch",
                    Detail = "The campaign ID in the route must match the campaign ID in the request body",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Validate the request
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(request);
            
            if (!Validator.TryValidateObject(request, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults.Select(v => v.ErrorMessage).ToArray();
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Validation Failed",
                    Detail = string.Join("; ", errors),
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var result = await approvalService.ProcessBulkApprovalAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing bulk approval for campaign: {CampaignId}", campaignId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while processing the bulk approval",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get pending approvals summary across all campaigns
    /// </summary>
    private static async Task<IResult> GetPendingApprovalsSummaryAsync(
        [FromServices] IApprovalService approvalService,
        [FromServices] ILogger<IApprovalService> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await approvalService.GetPendingApprovalsSummaryAsync(cancellationToken);
            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting pending approvals summary");
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while retrieving the pending approvals summary",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get pending approvals summary for a specific campaign
    /// </summary>
    private static async Task<IResult> GetCampaignPendingApprovalsAsync(
        [FromRoute] string campaignId,
        [FromServices] IApprovalService approvalService,
        [FromServices] ILogger<IApprovalService> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(campaignId))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Campaign ID",
                    Detail = "Campaign ID cannot be null or empty",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var summary = await approvalService.GetCampaignPendingApprovalsAsync(campaignId, cancellationToken);
            
            if (summary == null)
            {
                return Results.NotFound(new ProblemDetails
                {
                    Title = "Campaign Not Found",
                    Detail = $"No pending approvals found for campaign '{campaignId}'",
                    Status = StatusCodes.Status404NotFound
                });
            }

            return Results.Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting pending approvals for campaign: {CampaignId}", campaignId);
            return Results.Problem(
                title: "Internal Server Error",
                detail: "An error occurred while retrieving the campaign pending approvals",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
