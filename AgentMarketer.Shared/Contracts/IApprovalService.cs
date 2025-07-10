namespace AgentMarketer.Shared.Contracts;

/// <summary>
/// Service for managing company brief approvals
/// </summary>
public interface IApprovalService
{
    /// <summary>
    /// Get all company briefs for a campaign
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of company briefs</returns>
    Task<List<CompanyBriefResponse>> GetCompanyBriefsAsync(string campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get briefs that are pending approval
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending briefs</returns>
    Task<List<CompanyBriefResponse>> GetPendingBriefsAsync(string campaignId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a specific company brief
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="companyId">Company identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company brief details or null if not found</returns>
    Task<CompanyBriefResponse?> GetCompanyBriefAsync(string campaignId, string companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve or reject a company brief
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="companyId">Company identifier</param>
    /// <param name="request">Approval decision and feedback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approval result</returns>
    Task<ApprovalActionResponse> ProcessApprovalAsync(string campaignId, string companyId, ApprovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform bulk approval actions on multiple company briefs
    /// </summary>
    /// <param name="request">Bulk approval request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Bulk approval result</returns>
    Task<BulkApprovalResponse> ProcessBulkApprovalAsync(BulkApprovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get summary of pending approvals across all campaigns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pending approval summaries</returns>
    Task<List<PendingApprovalsSummary>> GetPendingApprovalsSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get summary of pending approvals for a specific campaign
    /// </summary>
    /// <param name="campaignId">Campaign identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pending approvals summary or null if campaign not found</returns>
    Task<PendingApprovalsSummary?> GetCampaignPendingApprovalsAsync(string campaignId, CancellationToken cancellationToken = default);
}
