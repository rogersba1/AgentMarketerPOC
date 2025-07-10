using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AgentMarketer.WebApi.Hubs;
using AgentMarketer.Shared.DTOs;
using System.Text.Json;

namespace AgentMarketer.WebApi.Endpoints;

/// <summary>
/// Chat-based agent orchestration endpoints
/// </summary>
public static class ChatEndpoints
{
    public static void MapChatEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/chat")
            .WithTags("Chat")
            .WithOpenApi();

        group.MapPost("/process", ProcessMessage)
            .WithName("ProcessChatMessage")
            .WithSummary("Process user chat message through agent orchestration")
            .WithDescription("Processes a user message through the multi-agent system and returns agent responses")
            .Produces<ChatResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/session/{sessionId}/approve", HandleApproval)
            .WithName("HandleChatApproval")
            .WithSummary("Handle approval decision from chat interface")
            .WithDescription("Processes approval decisions made through the chat interface")
            .Produces<ChatResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Process user message through agent orchestration
    /// </summary>
    private static async Task<IResult> ProcessMessage(
        [FromBody] ChatRequest request,
        [FromServices] IHubContext<CampaignHub> hubContext,
        [FromServices] ILogger<object> logger)
    {
        try
        {
            logger.LogInformation("Processing chat message for session {SessionId}: {Message}", 
                request.SessionId, request.Message);

            // For now, simulate agent processing
            // TODO: Integrate with actual agent orchestration service
            var response = await SimulateAgentProcessing(request, hubContext, logger);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing chat message");
            return Results.Problem(
                title: "Chat Processing Error",
                detail: "An error occurred while processing your message.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Handle approval decision from chat interface
    /// </summary>
    private static async Task<IResult> HandleApproval(
        string sessionId,
        [FromBody] ChatApprovalRequest request,
        [FromServices] IHubContext<CampaignHub> hubContext,
        [FromServices] ILogger<object> logger)
    {
        try
        {
            logger.LogInformation("Processing approval for session {SessionId}: Approved={Approved}", 
                sessionId, request.Approved);

            // TODO: Process approval through the actual system
            var response = new ChatResponse
            {
                SessionId = sessionId,
                AgentName = "Router Agent",
                Message = request.Approved 
                    ? "Thank you for your approval! I'm proceeding with the campaign execution."
                    : "I understand. Could you provide more details about what changes you'd like?",
                MessageType = ChatMessageType.AgentResponse
            };

            // Notify connected clients
            await hubContext.Clients.Group($"session:{sessionId}")
                .SendAsync("AgentMessage", response.AgentName, response.Message);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing approval");
            return Results.Problem(
                title: "Approval Processing Error",
                detail: "An error occurred while processing your approval.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Simulate agent processing for demo purposes
    /// TODO: Replace with actual agent orchestration
    /// </summary>
    private static async Task<ChatResponse> SimulateAgentProcessing(
        ChatRequest request, 
        IHubContext<CampaignHub> hubContext,
        ILogger logger)
    {
        // Join session group for real-time updates
        // Note: In a real implementation, this would be handled by the SignalR connection
        
        var message = request.Message.ToLower();
        
        if (message.Contains("campaign") || message.Contains("marketing"))
        {
            // Simulate planner agent response
            await Task.Delay(1000);
            await hubContext.Clients.Group($"session:{request.SessionId}")
                .SendAsync("AgentMessage", "Planner Agent", "Analyzing your campaign requirements...");

            await Task.Delay(2000);
            await hubContext.Clients.Group($"session:{request.SessionId}")
                .SendAsync("ProgressUpdate", "Campaign Analysis", 25);

            await Task.Delay(1500);
            await hubContext.Clients.Group($"session:{request.SessionId}")
                .SendAsync("AgentMessage", "Researcher Agent", "Identifying target companies and analyzing market data...");

            await Task.Delay(2000);
            await hubContext.Clients.Group($"session:{request.SessionId}")
                .SendAsync("ProgressUpdate", "Market Research", 75);

            // Return final response with approval request
            return new ChatResponse
            {
                SessionId = request.SessionId,
                AgentName = "Planner Agent",
                Message = "I've created a comprehensive campaign plan based on your requirements. The plan includes targeted content for 12 companies, email campaigns, and social media strategy. Would you like to review and approve this plan?",
                MessageType = ChatMessageType.Approval,
                RequiresApproval = true,
                Data = new
                {
                    CampaignSummary = new
                    {
                        TargetCompanies = 12,
                        Components = new[] { "Landing Pages", "Email Campaigns", "Social Media" },
                        Timeline = "2-3 weeks",
                        EstimatedBudget = "$15,000 - $25,000"
                    }
                }
            };
        }
        else
        {
            return new ChatResponse
            {
                SessionId = request.SessionId,
                AgentName = "Planner Agent",
                Message = "I'd be happy to help you create a marketing campaign. Could you tell me more about your goals? For example:\n\n• What product or service are you promoting?\n• Who is your target audience?\n• What marketing channels interest you (email, social media, landing pages)?\n• Do you have any budget or timeline constraints?",
                MessageType = ChatMessageType.AgentResponse
            };
        }
    }
}

/// <summary>
/// Request to process user chat message
/// </summary>
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

/// <summary>
/// Response from chat processing
/// </summary>
public class ChatResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ChatMessageType MessageType { get; set; }
    public object? Data { get; set; }
    public bool RequiresApproval { get; set; }
    public string? ApprovalId { get; set; }
}

/// <summary>
/// Types of chat messages
/// </summary>
public enum ChatMessageType
{
    AgentResponse,
    Progress,
    Approval,
    Error,
    Success
}

/// <summary>
/// Request for approval decision from chat
/// </summary>
public class ChatApprovalRequest
{
    public bool Approved { get; set; }
    public string? Feedback { get; set; }
}
