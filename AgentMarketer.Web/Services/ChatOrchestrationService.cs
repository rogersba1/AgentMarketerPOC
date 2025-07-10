using Microsoft.AspNetCore.SignalR.Client;
using AgentMarketer.Shared.DTOs;
using AgentMarketer.Shared.Models;
using System.Text.Json;

namespace AgentMarketer.Web.Services;

/// <summary>
/// Service for orchestrating agent-based campaign creation through chat interface
/// </summary>
public class ChatOrchestrationService : IAsyncDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatOrchestrationService> _logger;
    private HubConnection? _hubConnection;
    private readonly Dictionary<string, TaskCompletionSource<object>> _pendingRequests = new();

    public ChatOrchestrationService(HttpClient httpClient, ILogger<ChatOrchestrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public event Func<string, string, Task>? OnAgentMessage;
    public event Func<string, int, Task>? OnProgressUpdate;
    public event Func<object, Task>? OnApprovalRequired;

    /// <summary>
    /// Initialize SignalR connection for real-time updates
    /// </summary>
    public async Task InitializeAsync(string baseUrl)
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/chathub")
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string, string>("AgentMessage", async (agentName, message) =>
            {
                if (OnAgentMessage != null)
                    await OnAgentMessage(agentName, message);
            });

            _hubConnection.On<string, int>("ProgressUpdate", async (task, progress) =>
            {
                if (OnProgressUpdate != null)
                    await OnProgressUpdate(task, progress);
            });

            _hubConnection.On<object>("ApprovalRequired", async (approval) =>
            {
                if (OnApprovalRequired != null)
                    await OnApprovalRequired(approval);
            });

            await _hubConnection.StartAsync();
            _logger.LogInformation("SignalR connection established");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SignalR connection");
        }
    }

    /// <summary>
    /// Process user message through agent orchestration
    /// </summary>
    public async Task<ChatResponse> ProcessUserMessageAsync(string userMessage, string? sessionId = null)
    {
        try
        {
            var request = new ChatRequest
            {
                Message = userMessage,
                SessionId = sessionId ?? Guid.NewGuid().ToString()
            };

            var response = await _httpClient.PostAsJsonAsync("/api/chat/process", request);
            response.EnsureSuccessStatusCode();

            var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
            return chatResponse ?? throw new InvalidOperationException("Invalid response from chat API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message: {Message}", userMessage);
            return new ChatResponse
            {
                SessionId = sessionId ?? Guid.NewGuid().ToString(),
                AgentName = "System",
                Message = "I apologize, but I encountered an error processing your message. Please try again.",
                MessageType = ChatMessageType.Error
            };
        }
    }

    /// <summary>
    /// Create a new campaign based on chat conversation
    /// </summary>
    public async Task<CampaignResponse> CreateCampaignAsync(CreateCampaignRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/campaigns", request);
            response.EnsureSuccessStatusCode();

            var campaign = await response.Content.ReadFromJsonAsync<CampaignResponse>();
            return campaign ?? throw new InvalidOperationException("Invalid response from campaign API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign: {Name}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Submit approval decision
    /// </summary>
    public async Task<ApprovalResponse> SubmitApprovalAsync(string campaignId, string companyId, bool approved, string? feedback = null)
    {
        try
        {
            var request = new ApprovalRequest
            {
                IsApproved = approved,
                Feedback = feedback ?? string.Empty,
                Action = approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected
            };

            var response = await _httpClient.PostAsJsonAsync($"/api/campaigns/{campaignId}/companies/{companyId}/approve", request);
            response.EnsureSuccessStatusCode();

            var approval = await response.Content.ReadFromJsonAsync<ApprovalResponse>();
            return approval ?? throw new InvalidOperationException("Invalid response from approval API");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting approval for company {CompanyId} in campaign {CampaignId}", companyId, campaignId);
            throw;
        }
    }

    /// <summary>
    /// Get campaign status and progress
    /// </summary>
    public async Task<CampaignResponse> GetCampaignStatusAsync(string campaignId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/campaigns/{campaignId}");
            response.EnsureSuccessStatusCode();

            var campaign = await response.Content.ReadFromJsonAsync<CampaignResponse>();
            return campaign ?? throw new InvalidOperationException("Campaign not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting campaign status: {CampaignId}", campaignId);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
        _httpClient.Dispose();
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
