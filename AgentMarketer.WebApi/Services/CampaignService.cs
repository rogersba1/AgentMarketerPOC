using AgentMarketer.Shared.Contracts;
using AgentMarketer.Shared.DTOs;
using AgentMarketer.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using AgentMarketer.WebApi.Hubs;

namespace AgentMarketer.WebApi.Services;

/// <summary>
/// Service for managing marketing campaigns with Redis storage and real-time updates
/// </summary>
public class CampaignService : ICampaignService
{
    private readonly IRedisService _redisService;
    private readonly IHubContext<CampaignHub> _hubContext;
    private readonly ILogger<CampaignService> _logger;

    private const string CampaignKeyPrefix = "campaign:";
    private const string CampaignListKey = "campaigns";
    private const string ExecutionStatusKeyPrefix = "execution:";
    private const string PlanKeyPrefix = "plan:";

    public CampaignService(
        IRedisService redisService,
        IHubContext<CampaignHub> hubContext,
        ILogger<CampaignService> logger)
    {
        _redisService = redisService;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new marketing campaign
    /// </summary>
    public async Task<CampaignResponse> CreateCampaignAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new campaign: {CampaignName}", request.Name);

        var campaignId = Guid.NewGuid().ToString();
        var campaign = new Campaign
        {
            Id = campaignId,
            Name = request.Name,
            Goal = request.Goal,
            Audience = request.Audience,
            Components = request.Components,
            Status = CampaignStatus.Created,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "user", // TODO: Get from authentication context
            CompanyIds = new List<string>()
        };

        // Store campaign in Redis
        await _redisService.SetAsync($"{CampaignKeyPrefix}{campaignId}", campaign, cancellationToken: cancellationToken);

        // Add to campaigns list
        await _redisService.ListAddAsync(CampaignListKey, new[] { campaignId }, cancellationToken);

        var response = MapToResponse(campaign);

        // Notify clients via SignalR
        await _hubContext.Clients.All.SendAsync("CampaignCreated", response, cancellationToken);

        _logger.LogInformation("Campaign created successfully: {CampaignId}", campaignId);
        return response;
    }

    /// <summary>
    /// Get all campaigns for the current user
    /// </summary>
    public async Task<List<CampaignSummaryResponse>> GetCampaignsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all campaigns");

        var campaignIds = await _redisService.ListGetAllAsync<string>(CampaignListKey, cancellationToken);
        var campaigns = new List<CampaignSummaryResponse>();

        foreach (var campaignId in campaignIds)
        {
            var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}", cancellationToken);
            if (campaign != null)
            {
                campaigns.Add(MapToSummaryResponse(campaign));
            }
        }

        _logger.LogInformation("Retrieved {CampaignCount} campaigns", campaigns.Count);
        return campaigns.OrderByDescending(c => c.CreatedAt).ToList();
    }

    /// <summary>
    /// Get detailed information about a specific campaign
    /// </summary>
    public async Task<CampaignResponse?> GetCampaignAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving campaign: {CampaignId}", campaignId);

        var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}", cancellationToken);
        
        if (campaign == null)
        {
            _logger.LogWarning("Campaign not found: {CampaignId}", campaignId);
            return null;
        }

        return MapToResponse(campaign);
    }

    /// <summary>
    /// Create an execution plan for a campaign
    /// </summary>
    public async Task<CreatePlanResponse> CreatePlanAsync(string campaignId, CreatePlanRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating plan for campaign: {CampaignId}", campaignId);

        var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}", cancellationToken);
        if (campaign == null)
        {
            return new CreatePlanResponse(false, "Campaign not found", 0);
        }

        try
        {
            // Generate target companies (mock implementation)
            var targetCompanies = await GenerateTargetCompanies(campaign, request.CompanyNames);
            
            // Update campaign with target companies
            campaign.CompanyIds = targetCompanies;
            campaign.Status = CampaignStatus.Planned;
            await _redisService.SetAsync($"{CampaignKeyPrefix}{campaignId}", campaign, cancellationToken: cancellationToken);

            // Create execution plan (mock implementation)
            var planSteps = GenerateExecutionPlan(campaign);
            await _redisService.SetAsync($"{PlanKeyPrefix}{campaignId}", planSteps, cancellationToken: cancellationToken);

            // Initialize execution status
            var executionStatus = new ExecutionStatus
            {
                CampaignId = campaignId,
                Status = "Planned",
                Message = "Campaign plan created successfully",
                Progress = 0,
                CompletedSteps = 0,
                TotalSteps = planSteps.Count,
                CurrentStep = null,
                PendingApprovals = 0
            };
            await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", executionStatus, cancellationToken: cancellationToken);

            var response = new CreatePlanResponse(true, "Plan created successfully", planSteps.Count);

            // Notify clients
            await _hubContext.Clients.All.SendAsync("CampaignPlanCreated", campaignId, response, cancellationToken);

            _logger.LogInformation("Plan created for campaign {CampaignId} with {StepCount} steps", campaignId, planSteps.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan for campaign: {CampaignId}", campaignId);
            return new CreatePlanResponse(false, $"Error creating plan: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Start executing a campaign
    /// </summary>
    public async Task<ExecutionStatusResponse> StartExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting execution for campaign: {CampaignId}", campaignId);

        var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}", cancellationToken);
        if (campaign == null)
        {
            return new ExecutionStatusResponse("Error", "Campaign not found", 0, 0, 0, null, 0);
        }

        if (campaign.Status != CampaignStatus.Planned)
        {
            return new ExecutionStatusResponse("Error", "Campaign must be planned before execution", 0, 0, 0, null, 0);
        }

        // Update campaign status
        campaign.Status = CampaignStatus.Executing;
        campaign.ExecutedAt = DateTime.UtcNow;
        await _redisService.SetAsync($"{CampaignKeyPrefix}{campaignId}", campaign, cancellationToken: cancellationToken);

        // Update execution status
        var executionStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}", cancellationToken);
        if (executionStatus != null)
        {
            executionStatus.Status = "Executing";
            executionStatus.Message = "Campaign execution started";
            executionStatus.CurrentStep = "Initializing campaign execution";
            await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", executionStatus, cancellationToken: cancellationToken);
        }

        var response = new ExecutionStatusResponse(
            "Executing",
            "Campaign execution started",
            0,
            0,
            executionStatus?.TotalSteps ?? 0,
            "Initializing campaign execution",
            0
        );

        // Notify clients
        await _hubContext.Clients.All.SendAsync("CampaignExecutionStarted", campaignId, response, cancellationToken);

        // Start background execution (in a real implementation, this would be a background service)
        _ = Task.Run(async () => await SimulateExecution(campaignId), cancellationToken);

        return response;
    }

    /// <summary>
    /// Get current execution status for a campaign
    /// </summary>
    public async Task<ExecutionStatusResponse?> GetExecutionStatusAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        var executionStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}", cancellationToken);
        
        if (executionStatus == null)
        {
            return null;
        }

        return new ExecutionStatusResponse(
            executionStatus.Status,
            executionStatus.Message,
            executionStatus.Progress,
            executionStatus.CompletedSteps,
            executionStatus.TotalSteps,
            executionStatus.CurrentStep,
            executionStatus.PendingApprovals
        );
    }

    /// <summary>
    /// Pause campaign execution
    /// </summary>
    public async Task<ExecutionStatusResponse> PauseExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pausing execution for campaign: {CampaignId}", campaignId);

        var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}", cancellationToken);
        if (campaign == null)
        {
            return new ExecutionStatusResponse("Error", "Campaign not found", 0, 0, 0, null, 0);
        }

        campaign.Status = CampaignStatus.Paused;
        await _redisService.SetAsync($"{CampaignKeyPrefix}{campaignId}", campaign, cancellationToken: cancellationToken);

        var executionStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}", cancellationToken);
        if (executionStatus != null)
        {
            executionStatus.Status = "Paused";
            executionStatus.Message = "Campaign execution paused";
            await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", executionStatus, cancellationToken: cancellationToken);
        }

        var response = new ExecutionStatusResponse(
            "Paused",
            "Campaign execution paused",
            executionStatus?.Progress ?? 0,
            executionStatus?.CompletedSteps ?? 0,
            executionStatus?.TotalSteps ?? 0,
            executionStatus?.CurrentStep,
            executionStatus?.PendingApprovals ?? 0
        );

        await _hubContext.Clients.All.SendAsync("CampaignExecutionPaused", campaignId, response, cancellationToken);

        return response;
    }

    /// <summary>
    /// Resume campaign execution
    /// </summary>
    public async Task<ExecutionStatusResponse> ResumeExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming execution for campaign: {CampaignId}", campaignId);

        var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}", cancellationToken);
        if (campaign == null)
        {
            return new ExecutionStatusResponse("Error", "Campaign not found", 0, 0, 0, null, 0);
        }

        campaign.Status = CampaignStatus.Executing;
        await _redisService.SetAsync($"{CampaignKeyPrefix}{campaignId}", campaign, cancellationToken: cancellationToken);

        var executionStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}", cancellationToken);
        if (executionStatus != null)
        {
            executionStatus.Status = "Executing";
            executionStatus.Message = "Campaign execution resumed";
            await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", executionStatus, cancellationToken: cancellationToken);
        }

        var response = new ExecutionStatusResponse(
            "Executing",
            "Campaign execution resumed",
            executionStatus?.Progress ?? 0,
            executionStatus?.CompletedSteps ?? 0,
            executionStatus?.TotalSteps ?? 0,
            executionStatus?.CurrentStep,
            executionStatus?.PendingApprovals ?? 0
        );

        await _hubContext.Clients.All.SendAsync("CampaignExecutionResumed", campaignId, response, cancellationToken);

        return response;
    }

    /// <summary>
    /// Cancel campaign execution
    /// </summary>
    public async Task<ExecutionStatusResponse> CancelExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling execution for campaign: {CampaignId}", campaignId);

        var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}", cancellationToken);
        if (campaign == null)
        {
            return new ExecutionStatusResponse("Error", "Campaign not found", 0, 0, 0, null, 0);
        }

        campaign.Status = CampaignStatus.Cancelled;
        await _redisService.SetAsync($"{CampaignKeyPrefix}{campaignId}", campaign, cancellationToken: cancellationToken);

        var executionStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}", cancellationToken);
        if (executionStatus != null)
        {
            executionStatus.Status = "Cancelled";
            executionStatus.Message = "Campaign execution cancelled";
            await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", executionStatus, cancellationToken: cancellationToken);
        }

        var response = new ExecutionStatusResponse(
            "Cancelled",
            "Campaign execution cancelled",
            executionStatus?.Progress ?? 0,
            executionStatus?.CompletedSteps ?? 0,
            executionStatus?.TotalSteps ?? 0,
            null,
            0
        );

        await _hubContext.Clients.All.SendAsync("CampaignExecutionCancelled", campaignId, response, cancellationToken);

        return response;
    }

    #region Private Helper Methods

    private static CampaignResponse MapToResponse(Campaign campaign)
    {
        return new CampaignResponse(
            campaign.Id,
            campaign.Name,
            campaign.Goal,
            campaign.Audience,
            campaign.Components,
            campaign.Status.ToString(),
            campaign.CreatedAt,
            campaign.ExecutedAt,
            campaign.CreatedBy,
            campaign.CompanyIds.Count,
            0, // TODO: Calculate approved briefs
            0  // TODO: Calculate pending approvals
        );
    }

    private static CampaignSummaryResponse MapToSummaryResponse(Campaign campaign)
    {
        return new CampaignSummaryResponse(
            campaign.Id,
            campaign.Name,
            campaign.Goal,
            campaign.Status.ToString(),
            campaign.CreatedAt,
            campaign.CompanyIds.Count,
            0 // TODO: Calculate pending approvals
        );
    }

    private Task<List<string>> GenerateTargetCompanies(Campaign campaign, List<string>? specificCompanies)
    {
        // Mock implementation - in a real scenario, this would use the research agent
        if (specificCompanies?.Any() == true)
        {
            return Task.FromResult(specificCompanies);
        }

        // Generate mock company IDs based on campaign audience
        var companyCount = Random.Shared.Next(5, 15);
        var companies = new List<string>();
        
        for (int i = 0; i < companyCount; i++)
        {
            companies.Add($"company_{Guid.NewGuid():N}");
        }

        return Task.FromResult(companies);
    }

    private static List<ExecutionStep> GenerateExecutionPlan(Campaign campaign)
    {
        var steps = new List<ExecutionStep>();
        var stepId = 1;

        // Add steps based on campaign components
        foreach (var component in campaign.Components)
        {
            switch (component.ToLowerInvariant())
            {
                case "landing page":
                    steps.Add(new ExecutionStep { Id = stepId++, Name = "Create landing page briefs", Description = "Generate company-specific landing page briefs" });
                    steps.Add(new ExecutionStep { Id = stepId++, Name = "Review landing page content", Description = "Review and approve landing page designs" });
                    break;
                case "email":
                    steps.Add(new ExecutionStep { Id = stepId++, Name = "Create email campaigns", Description = "Generate personalized email content" });
                    steps.Add(new ExecutionStep { Id = stepId++, Name = "Review email content", Description = "Review and approve email campaigns" });
                    break;
                case "social media":
                    steps.Add(new ExecutionStep { Id = stepId++, Name = "Create social media content", Description = "Generate social media posts and campaigns" });
                    steps.Add(new ExecutionStep { Id = stepId++, Name = "Review social content", Description = "Review and approve social media content" });
                    break;
            }
        }

        steps.Add(new ExecutionStep { Id = stepId++, Name = "Deploy campaign", Description = "Deploy approved content to target channels" });
        steps.Add(new ExecutionStep { Id = stepId, Name = "Monitor performance", Description = "Track campaign performance and engagement" });

        return steps;
    }

    private async Task SimulateExecution(string campaignId)
    {
        try
        {
            var executionStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}");
            if (executionStatus == null) return;

            var steps = await _redisService.GetAsync<List<ExecutionStep>>($"{PlanKeyPrefix}{campaignId}");
            if (steps == null) return;

            foreach (var step in steps)
            {
                // Check if execution was paused or cancelled
                var currentStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}");
                if (currentStatus?.Status != "Executing") break;

                // Simulate step execution
                executionStatus.CurrentStep = step.Description;
                executionStatus.Progress = (int)((double)executionStatus.CompletedSteps / executionStatus.TotalSteps * 100);
                await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", executionStatus);

                // Notify clients of progress
                var statusResponse = new ExecutionStatusResponse(
                    executionStatus.Status,
                    executionStatus.Message,
                    executionStatus.Progress,
                    executionStatus.CompletedSteps,
                    executionStatus.TotalSteps,
                    executionStatus.CurrentStep,
                    executionStatus.PendingApprovals
                );
                await _hubContext.Clients.All.SendAsync("CampaignExecutionProgress", campaignId, statusResponse);

                // Simulate work delay
                await Task.Delay(Random.Shared.Next(2000, 5000));

                executionStatus.CompletedSteps++;
            }

            // Mark as completed if not cancelled/paused
            var finalStatus = await _redisService.GetAsync<ExecutionStatus>($"{ExecutionStatusKeyPrefix}{campaignId}");
            if (finalStatus?.Status == "Executing")
            {
                finalStatus.Status = "Completed";
                finalStatus.Message = "Campaign execution completed successfully";
                finalStatus.Progress = 100;
                finalStatus.CurrentStep = null;
                await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", finalStatus);

                // Update campaign status
                var campaign = await _redisService.GetAsync<Campaign>($"{CampaignKeyPrefix}{campaignId}");
                if (campaign != null)
                {
                    campaign.Status = CampaignStatus.Completed;
                    await _redisService.SetAsync($"{CampaignKeyPrefix}{campaignId}", campaign);
                }

                // Notify completion
                var completionResponse = new ExecutionStatusResponse(
                    "Completed",
                    "Campaign execution completed successfully",
                    100,
                    finalStatus.CompletedSteps,
                    finalStatus.TotalSteps,
                    null,
                    0
                );
                await _hubContext.Clients.All.SendAsync("CampaignExecutionCompleted", campaignId, completionResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during campaign execution simulation: {CampaignId}", campaignId);
            
            // Mark as failed
            var failedStatus = new ExecutionStatus
            {
                CampaignId = campaignId,
                Status = "Failed",
                Message = $"Execution failed: {ex.Message}",
                Progress = 0,
                CompletedSteps = 0,
                TotalSteps = 0,
                CurrentStep = null,
                PendingApprovals = 0
            };
            await _redisService.SetAsync($"{ExecutionStatusKeyPrefix}{campaignId}", failedStatus);

            var failureResponse = new ExecutionStatusResponse(
                "Failed",
                $"Execution failed: {ex.Message}",
                0,
                0,
                0,
                null,
                0
            );
            await _hubContext.Clients.All.SendAsync("CampaignExecutionFailed", campaignId, failureResponse);
        }
    }

    #endregion
}

/// <summary>
/// Internal model for execution status tracking
/// </summary>
internal class ExecutionStatus
{
    public string CampaignId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int CompletedSteps { get; set; }
    public int TotalSteps { get; set; }
    public string? CurrentStep { get; set; }
    public int PendingApprovals { get; set; }
}

/// <summary>
/// Internal model for execution steps
/// </summary>
internal class ExecutionStep
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
