using Microsoft.AspNetCore.Mvc;
using AgentOrchestration.Services.Modern;
using AgentOrchestration.Services;
using AgentOrchestration.Models;

namespace AgentMarketer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModernOrchestrationController : ControllerBase
{
    private readonly SequentialCampaignOrchestrationService _sequentialService;
    private readonly ContextPersistenceService _contextService;
    private readonly ILogger<ModernOrchestrationController> _logger;

    public ModernOrchestrationController(
        SequentialCampaignOrchestrationService sequentialService,
        ContextPersistenceService contextService,
        ILogger<ModernOrchestrationController> logger)
    {
        _sequentialService = sequentialService;
        _contextService = contextService;
        _logger = logger;
    }

    [HttpPost("execute-campaign")]
    public async Task<IActionResult> ExecuteCampaign([FromBody] ModernCampaignRequest request)
    {
        try
        {
            _logger.LogInformation("Starting sequential campaign execution for prompt: {Prompt}", request.UserPrompt);

            // Create or get session
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();
            var session = await _contextService.LoadSessionAsync(sessionId) ?? new CampaignSession
            {
                Id = sessionId,
                Campaign = new Campaign
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"Campaign {DateTime.Now:yyyy-MM-dd HH:mm}",
                    Goal = request.UserPrompt,
                    Status = CampaignStatus.InProgress,
                    CreatedAt = DateTime.UtcNow,
                    ExecutionLog = new List<string>()
                }
            };

            // Execute using Sequential Orchestration
            var result = await _sequentialService.ExecuteCampaignSequentiallyAsync(session, request.UserPrompt);
            
            _logger.LogInformation("Sequential campaign execution started in session {SessionId}", sessionId);

            return Ok(new ModernCampaignResponse
            {
                SessionId = sessionId,
                Status = "Campaign execution started",
                Message = result,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing sequential campaign: {Prompt}", request.UserPrompt);
            return StatusCode(500, new { error = "Failed to execute campaign", details = ex.Message });
        }
    }

    [HttpPost("collaborative-planning")]
    public async Task<IActionResult> CollaborativePlanning([FromBody] ModernCampaignRequest request)
    {
        try
        {
            _logger.LogInformation("Redirecting collaborative planning to sequential execution for prompt: {Prompt}", request.UserPrompt);

            // Use sequential orchestration instead of collaborative planning
            return await ExecuteCampaign(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sequential planning: {Prompt}", request.UserPrompt);
            return StatusCode(500, new { error = "Failed to start sequential planning", details = ex.Message });
        }
    }

    [HttpGet("status/{sessionId}")]
    public async Task<IActionResult> GetOrchestrationStatus(string sessionId)
    {
        try
        {
            _logger.LogInformation("Retrieving sequential orchestration status for session {SessionId}", sessionId);

            var session = await _sequentialService.GetSessionAsync(sessionId);
            
            if (session == null)
            {
                return NotFound($"Session {sessionId} not found");
            }

            return Ok(new ModernOrchestrationStatus
            {
                SessionId = sessionId,
                IsActive = session.IsActive,
                CurrentStage = session.Campaign.Status.ToString(),
                CompletedStages = new List<string> { "Company Discovery", "Brief Generation", "Human Approval Setup" },
                Messages = session.Campaign.ExecutionLog,
                LastActivity = session.LastUpdated,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestration status for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to retrieve orchestration status", details = ex.Message });
        }
    }

    [HttpGet("sessions")]
    public IActionResult GetActiveSessions()
    {
        try
        {
            _logger.LogInformation("Retrieving active sequential orchestration sessions");

            // For now, return a simple placeholder since we don't have a method to get all sessions
            // In a full implementation, this would query the persistence service for all active sessions
            
            return Ok(new
            {
                TotalSessions = 0,
                Sessions = new object[0],
                Message = "Session listing not implemented - use specific session ID to check status",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return StatusCode(500, new { error = "Failed to retrieve active sessions", details = ex.Message });
        }
    }
}

public class ModernCampaignRequest
{
    public string UserPrompt { get; set; } = "";
    public string? SessionId { get; set; }
}

public class ModernCampaignResponse
{
    public string SessionId { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class ModernOrchestrationStatus
{
    public string SessionId { get; set; } = "";
    public bool IsActive { get; set; }
    public string CurrentStage { get; set; } = "";
    public List<string> CompletedStages { get; set; } = new();
    public List<string> Messages { get; set; } = new();
    public DateTime LastActivity { get; set; }
    public DateTime Timestamp { get; set; }
}
