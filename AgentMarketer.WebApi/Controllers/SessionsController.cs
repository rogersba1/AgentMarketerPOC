using Microsoft.AspNetCore.Mvc;
using AgentMarketer.WebApi.Services;

namespace AgentMarketer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionsController : ControllerBase
{
    private readonly ChatOrchestrationBridge _chatBridge;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(ChatOrchestrationBridge chatBridge, ILogger<SessionsController> logger)
    {
        _chatBridge = chatBridge;
        _logger = logger;
    }

    /// <summary>
    /// Get all active campaign sessions
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> GetActiveSessions()
    {
        try
        {
            var sessions = await _chatBridge.GetActiveSessionsAsync();
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active sessions");
            return StatusCode(500, new { error = "Failed to retrieve sessions" });
        }
    }

    /// <summary>
    /// Get session history/details for a specific session
    /// </summary>
    [HttpGet("{sessionId}")]
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

    /// <summary>
    /// Get company briefs for a specific session
    /// </summary>
    [HttpGet("{sessionId}/briefs")]
    public async Task<IActionResult> GetSessionBriefs(string sessionId)
    {
        try
        {
            var briefs = await _chatBridge.GetSessionBriefsAsync(sessionId);
            return Ok(briefs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session briefs for {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to retrieve session briefs" });
        }
    }
}
