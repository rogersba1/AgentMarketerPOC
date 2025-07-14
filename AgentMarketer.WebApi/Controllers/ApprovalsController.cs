using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AgentOrchestration.Services;
using AgentOrchestration.Services.Modern;
using AgentMarketer.Shared.Models;

namespace AgentMarketer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly ILogger<ApprovalsController> _logger;
    private readonly SequentialCampaignOrchestrationService _orchestrationService;
    private readonly ContextPersistenceService _persistenceService;

    public ApprovalsController(
        ILogger<ApprovalsController> _logger,
        SequentialCampaignOrchestrationService orchestrationService,
        ContextPersistenceService persistenceService)
    {
        this._logger = _logger;
        _orchestrationService = orchestrationService;
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Approve or reject a company brief
    /// </summary>
    [HttpPost("campaigns/{campaignId}/briefs/{companyId}/approve")]
    public async Task<IActionResult> ProcessApproval(
        string campaignId, 
        string companyId, 
        [FromBody] ApprovalRequest request)
    {
        try
        {
            _logger.LogInformation($"Processing approval for campaign {campaignId}, company {companyId}, action: {request.Action}");

            // Get the session from the orchestration service
            var session = await _orchestrationService.GetSessionAsync(campaignId);
            if (session == null)
            {
                return NotFound($"Campaign session {campaignId} not found");
            }

            // Process the approval through the orchestration service
            string result;
            if (request.IsApproved)
            {
                if (!string.IsNullOrEmpty(request.ModifiedContent))
                {
                    // Approve with modifications
                    result = await _orchestrationService.ApproveCompanyBriefWithModificationsAsync(
                        campaignId, companyId, request.ModifiedContent, request.Feedback ?? "");
                }
                else
                {
                    // Standard approval
                    result = await _orchestrationService.ApproveCompanyBriefAsync(
                        campaignId, companyId, request.Feedback ?? "");
                }
            }
            else
            {
                // Rejection
                result = await _orchestrationService.RejectCompanyBriefAsync(
                    campaignId, companyId, request.Feedback ?? "Rejected by user");
            }

            return Ok(new { 
                Success = true, 
                Message = result,
                CompanyId = companyId,
                Status = request.Action.ToString()
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Invalid request for campaign {campaignId}, company {companyId}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing approval for campaign {campaignId}, company {companyId}");
            return StatusCode(500, "Internal server error processing approval");
        }
    }

    /// <summary>
    /// Get all company briefs for a campaign
    /// </summary>
    [HttpGet("campaigns/{campaignId}/briefs")]
    public async Task<IActionResult> GetCompanyBriefs(string campaignId)
    {
        try
        {
            var session = await _orchestrationService.GetSessionAsync(campaignId);
            if (session == null)
            {
                return NotFound($"Campaign session {campaignId} not found");
            }

            // Get pending approvals from the session
            var briefs = await _orchestrationService.GetPendingCompanyBriefs(campaignId);
            
            return Ok(briefs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting company briefs for campaign {campaignId}");
            return StatusCode(500, "Internal server error getting company briefs");
        }
    }

    /// <summary>
    /// Continue campaign execution after approvals
    /// </summary>
    [HttpPost("campaigns/{campaignId}/continue")]
    public async Task<IActionResult> ContinueCampaign(string campaignId)
    {
        try
        {
            var result = await _orchestrationService.ContinueCampaignExecutionAsync(campaignId);
            return Ok(new { Success = true, Message = result });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Invalid request to continue campaign {campaignId}");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error continuing campaign {campaignId}");
            return StatusCode(500, "Internal server error continuing campaign");
        }
    }

}
