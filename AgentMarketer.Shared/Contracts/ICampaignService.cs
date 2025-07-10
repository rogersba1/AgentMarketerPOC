namespace AgentMarketer.Shared.Contracts;

/// <summary>
/// Service for managing campaigns in the web interface
/// </summary>
public interface ICampaignService
{
    /// <summary>
    /// Create a new marketing campaign
    /// </summary>
    /// <param name="request">Campaign creation details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created campaign details</returns>
    Task<CampaignResponse> CreateCampaignAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all campaigns for the current user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of campaign summaries</returns>
    Task<List<CampaignSummaryResponse>> GetCampaignsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed information about a specific campaign
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Campaign details or null if not found</returns>
    Task<CampaignResponse?> GetCampaignAsync(string campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create an execution plan for a campaign
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="request">Plan creation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plan creation result</returns>
    Task<CreatePlanResponse> CreatePlanAsync(string campaignId, CreatePlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start executing a campaign
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution start result</returns>
    Task<ExecutionStatusResponse> StartExecutionAsync(string campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current execution status for a campaign
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current execution status</returns>
    Task<ExecutionStatusResponse?> GetExecutionStatusAsync(string campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pause campaign execution
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated execution status</returns>
    Task<ExecutionStatusResponse> PauseExecutionAsync(string campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume campaign execution
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated execution status</returns>
    Task<ExecutionStatusResponse> ResumeExecutionAsync(string campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel campaign execution
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated execution status</returns>
    Task<ExecutionStatusResponse> CancelExecutionAsync(string campaignId, CancellationToken cancellationToken = default);
}
