using Microsoft.AspNetCore.SignalR;

namespace AgentMarketer.WebApi.Hubs;

/// <summary>
/// SignalR hub for real-time campaign updates
/// </summary>
public class CampaignHub : Hub
{
    private readonly ILogger<CampaignHub> _logger;

    public CampaignHub(ILogger<CampaignHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Join a campaign group to receive updates for a specific campaign
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    public async Task JoinCampaignGroup(string campaignId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"campaign:{campaignId}");
        _logger.LogInformation("Connection {ConnectionId} joined campaign group: {CampaignId}", 
            Context.ConnectionId, campaignId);
    }

    /// <summary>
    /// Leave a campaign group
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    public async Task LeaveCampaignGroup(string campaignId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign:{campaignId}");
        _logger.LogInformation("Connection {ConnectionId} left campaign group: {CampaignId}", 
            Context.ConnectionId, campaignId);
    }

    /// <summary>
    /// Join the approvals group to receive approval notifications
    /// </summary>
    public async Task JoinApprovalsGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "approvals");
        _logger.LogInformation("Connection {ConnectionId} joined approvals group", Context.ConnectionId);
    }

    /// <summary>
    /// Leave the approvals group
    /// </summary>
    public async Task LeaveApprovalsGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "approvals");
        _logger.LogInformation("Connection {ConnectionId} left approvals group", Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
