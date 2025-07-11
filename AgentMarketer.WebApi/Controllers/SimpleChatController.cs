using Microsoft.AspNetCore.Mvc;
using AgentMarketer.WebApi.Services;

namespace AgentMarketer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SimpleChatController : ControllerBase
{
    private readonly ChatOrchestrationBridge _chatBridge;
    private readonly ILogger<SimpleChatController> _logger;

    public SimpleChatController(ChatOrchestrationBridge chatBridge, ILogger<SimpleChatController> logger)
    {
        _chatBridge = chatBridge;
        _logger = logger;
    }

    [HttpPost("message")]
    public async Task<IActionResult> ProcessMessage([FromBody] SimpleChatRequest request)
    {
        try
        {
            _logger.LogInformation("Processing chat message for session {SessionId}: {Message}", 
                request.SessionId ?? "new", request.Message);

            var response = await _chatBridge.ProcessUserMessageAsync(request.Message, request.SessionId);
            
            _logger.LogInformation("Chat response generated for session {SessionId} by agent {AgentName}", 
                response.SessionId, response.AgentName);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message: {Message}", request.Message);
            return StatusCode(500, new { error = "Failed to process message", details = ex.Message });
        }
    }

    [HttpGet("session/{sessionId}")]
    public IActionResult GetSessionHistory(string sessionId)
    {
        try
        {
            // This could be expanded to return session history if needed
            return Ok(new { sessionId = sessionId, status = "active" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session history for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to retrieve session history" });
        }
    }

    [HttpPost("approvals/campaigns/{campaignId}/briefs/{companyId}/approve")]
    public IActionResult ApproveBrief(string campaignId, string companyId, [FromBody] ApprovalRequest request)
    {
        try
        {
            _logger.LogInformation("Processing approval for company {CompanyId} in campaign {CampaignId}: {Action}", 
                companyId, campaignId, request.Action);

            // For now, we'll just return success - this needs to be connected to the orchestration system
            // TODO: Connect to ChatOrchestrationBridge to process approval and update session
            
            var response = new
            {
                success = true,
                companyId = companyId,
                campaignId = campaignId,
                status = request.Action.ToString(),
                message = $"Company brief for {companyId} has been {request.Action.ToString().ToLower()}"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval for company {CompanyId}", companyId);
            return StatusCode(500, new { error = "Failed to process approval", details = ex.Message });
        }
    }
}

public class SimpleChatRequest
{
    public string Message { get; set; } = "";
    public string? SessionId { get; set; }
}

public class ApprovalRequest
{
    public ApprovalStatus Action { get; set; }
    public string Feedback { get; set; } = "";
    public bool IsApproved { get; set; }
    public string ApprovedBy { get; set; } = "";
    public string? ModifiedContent { get; set; }
}

public enum ApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
