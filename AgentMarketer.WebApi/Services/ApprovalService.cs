using AgentMarketer.Shared.Contracts;
using AgentMarketer.Shared.DTOs;
using AgentMarketer.Shared.Models;
using OrchestrationModels = AgentOrchestration.Models;

namespace AgentMarketer.WebApi.Services;

/// <summary>
/// Service for managing company brief approvals
/// </summary>
public class ApprovalService : IApprovalService
{
    private readonly IRedisService _redisService;
    private readonly ILogger<ApprovalService> _logger;

    private const string CompanyBriefsKey = "campaign:{0}:company-briefs";
    private const string PendingApprovalsKey = "pending-approvals";
    private const string ApprovalHistoryKey = "campaign:{0}:company:{1}:approval-history";

    public ApprovalService(IRedisService redisService, ILogger<ApprovalService> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    public async Task<List<CompanyBriefResponse>> GetCompanyBriefsAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting company briefs for campaign: {CampaignId}", campaignId);

            var key = string.Format(CompanyBriefsKey, campaignId);
            var briefs = await _redisService.ListGetAllAsync<CompanyBriefResponse>(key, cancellationToken);

            return briefs ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company briefs for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<List<CompanyBriefResponse>> GetPendingBriefsAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pending briefs for campaign: {CampaignId}", campaignId);

            var allBriefs = await GetCompanyBriefsAsync(campaignId, cancellationToken);
            return allBriefs.Where(b => b.Status == ApprovalStatus.Pending).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending briefs for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<CompanyBriefResponse?> GetCompanyBriefAsync(string campaignId, string companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting company brief for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);

            var briefs = await GetCompanyBriefsAsync(campaignId, cancellationToken);
            return briefs.FirstOrDefault(b => b.CompanyId == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company brief for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            throw;
        }
    }

    public async Task<ApprovalActionResponse> ProcessApprovalAsync(string campaignId, string companyId, ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing approval for campaign: {CampaignId}, company: {CompanyId}, action: {Action}", 
                campaignId, companyId, request.Action);

            // Get the current brief
            var brief = await GetCompanyBriefAsync(campaignId, companyId, cancellationToken);
            if (brief == null)
            {
                return new ApprovalResponse
                {
                    Success = false,
                    Message = "Company brief not found",
                    ApprovalId = Guid.NewGuid().ToString(),
                    ProcessedAt = DateTime.UtcNow
                };
            }

            // Update the brief status - we need to create a new record since properties are init-only
            var updatedBrief = brief with 
            { 
                Status = request.Action,
                ApproverFeedback = request.Feedback,
                ApprovedAt = DateTime.UtcNow,
                ApprovedBy = request.ApprovedBy
            };

            // Save the updated brief
            await UpdateCompanyBriefAsync(campaignId, updatedBrief, cancellationToken);

            // Save approval history
            await SaveApprovalHistoryAsync(campaignId, companyId, request, cancellationToken);

            // Update pending approvals summary
            await UpdatePendingApprovalsSummaryAsync(campaignId, cancellationToken);

            // Publish approval event for real-time updates
            await _redisService.PublishAsync($"approval:{campaignId}", new
            {
                CampaignId = campaignId,
                CompanyId = companyId,
                Action = request.Action,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            return new ApprovalResponse
            {
                Success = true,
                Message = $"Company brief {request.Action.ToString().ToLower()}",
                ApprovalId = Guid.NewGuid().ToString(),
                ProcessedAt = DateTime.UtcNow,
                UpdatedBrief = updatedBrief
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            throw;
        }
    }

    public async Task<BulkApprovalResponse> ProcessBulkApprovalAsync(BulkApprovalRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing bulk approval for campaign: {CampaignId}, {Count} items", 
                request.CampaignId, request.Approvals.Count);

            var results = new List<ApprovalActionResponse>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var approval in request.Approvals)
            {
                try
                {
                    var result = await ProcessApprovalAsync(request.CampaignId, approval.CompanyId, 
                        new ApprovalRequest
                        {
                            Action = approval.Action,
                            Feedback = approval.Feedback ?? string.Empty,
                            ApprovedBy = request.ApprovedBy,
                            IsApproved = approval.Action == ApprovalStatus.Approved
                        }, cancellationToken);

                    results.Add(result);
                    if (result.Success) successCount++;
                    else failureCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing bulk approval item for company: {CompanyId}", approval.CompanyId);
                    results.Add(new ApprovalResponse
                    {
                        Success = false,
                        Message = $"Error processing approval: {ex.Message}",
                        ApprovalId = Guid.NewGuid().ToString(),
                        ProcessedAt = DateTime.UtcNow
                    });
                    failureCount++;
                }
            }

            return new BulkApprovalResponse
            {
                Success = failureCount == 0,
                Message = $"Processed {successCount} successfully, {failureCount} failed",
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalCount = request.Approvals.Count,
                Results = results,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk approval for campaign: {CampaignId}", request.CampaignId);
            throw;
        }
    }

    public async Task<List<PendingApprovalsSummary>> GetPendingApprovalsSummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pending approvals summary");

            var summaries = await _redisService.ListGetAllAsync<PendingApprovalsSummary>(PendingApprovalsKey, cancellationToken);
            return summaries ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals summary");
            throw;
        }
    }

    public async Task<PendingApprovalsSummary?> GetCampaignPendingApprovalsAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting pending approvals for campaign: {CampaignId}", campaignId);

            var allSummaries = await GetPendingApprovalsSummaryAsync(cancellationToken);
            return allSummaries.FirstOrDefault(s => s.CampaignId == campaignId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    /// <summary>
    /// Generate company briefs for a campaign (used by the campaign service)
    /// </summary>
    public async Task GenerateCompanyBriefsAsync(string campaignId, Campaign campaign, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating company briefs for campaign: {CampaignId}", campaignId);

            var briefs = new List<CompanyBriefResponse>();

            // Generate briefs for target companies (mock implementation)
            var targetCompanies = new[]
            {
                new { Id = "company-1", Name = "TechStart Inc.", Industry = "Technology" },
                new { Id = "company-2", Name = "Green Energy Co.", Industry = "Renewable Energy" },
                new { Id = "company-3", Name = "FinanceFirst", Industry = "Financial Services" }
            };

            foreach (var company in targetCompanies)
            {
                var brief = new CompanyBriefResponse
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    Brief = GenerateMockBriefContent(company.Name, company.Industry, campaign),
                    Industry = company.Industry,
                    CampaignId = campaignId,
                    Status = ApprovalStatus.Pending,
                    GeneratedAt = DateTime.UtcNow,
                    Content = GenerateMockBriefContent(company.Name, company.Industry, campaign),
                    KeyMessages = GenerateMockKeyMessages(company.Industry),
                    TargetAudience = GenerateMockTargetAudience(company.Industry),
                    EstimatedBudget = Random.Shared.Next(10000, 100000),
                    ProjectedReach = Random.Shared.Next(50000, 500000)
                };

                briefs.Add(brief);
            }

            // Save the briefs to Redis
            var key = string.Format(CompanyBriefsKey, campaignId);
            await _redisService.SetAsync(key, briefs, TimeSpan.FromDays(30), cancellationToken);

            // Update pending approvals summary
            await UpdatePendingApprovalsSummaryAsync(campaignId, cancellationToken);

            _logger.LogInformation("Generated {Count} company briefs for campaign: {CampaignId}", briefs.Count, campaignId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating company briefs for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    private async Task UpdateCompanyBriefAsync(string campaignId, CompanyBriefResponse brief, CancellationToken cancellationToken)
    {
        var briefs = await GetCompanyBriefsAsync(campaignId, cancellationToken);
        var index = briefs.FindIndex(b => b.CompanyId == brief.CompanyId);
        
        if (index >= 0)
        {
            briefs[index] = brief;
            var key = string.Format(CompanyBriefsKey, campaignId);
            await _redisService.SetAsync(key, briefs, TimeSpan.FromDays(30), cancellationToken);
        }
    }

    private async Task SaveApprovalHistoryAsync(string campaignId, string companyId, ApprovalRequest request, CancellationToken cancellationToken)
    {
        var historyKey = string.Format(ApprovalHistoryKey, campaignId, companyId);
        var history = await _redisService.ListGetAllAsync<ApprovalHistoryEntry>(historyKey, cancellationToken) ?? [];
        
        history.Add(new ApprovalHistoryEntry
        {
            Action = request.Action,
            Feedback = request.Feedback,
            ApprovedBy = request.ApprovedBy,
            Timestamp = DateTime.UtcNow
        });

        await _redisService.SetAsync(historyKey, history, TimeSpan.FromDays(90), cancellationToken);
    }

    private async Task UpdatePendingApprovalsSummaryAsync(string campaignId, CancellationToken cancellationToken)
    {
        var pendingBriefs = await GetPendingBriefsAsync(campaignId, cancellationToken);
        var summaries = await GetPendingApprovalsSummaryAsync(cancellationToken);
        
        var existingSummary = summaries.FirstOrDefault(s => s.CampaignId == campaignId);
        if (existingSummary != null)
        {
            summaries.Remove(existingSummary);
        }

        if (pendingBriefs.Count > 0)
        {
            summaries.Add(new PendingApprovalsSummary
            {
                CampaignId = campaignId,
                CampaignName = $"Campaign {campaignId}", // TODO: Get actual campaign name
                PendingCount = pendingBriefs.Count,
                TotalCount = (await GetCompanyBriefsAsync(campaignId, cancellationToken)).Count,
                OldestPending = pendingBriefs.Min(b => b.GeneratedAt),
                OldestPendingDate = pendingBriefs.Min(b => b.GeneratedAt),
                LastUpdated = DateTime.UtcNow
            });
        }

        await _redisService.SetAsync(PendingApprovalsKey, summaries, TimeSpan.FromDays(30), cancellationToken);
    }

    private static string GenerateMockBriefContent(string companyName, string industry, Campaign campaign)
    {
        return $"""
            # Marketing Campaign Brief for {companyName}

            ## Campaign Overview
            **Campaign:** {campaign.Name}
            **Target Company:** {companyName}
            **Industry:** {industry}
            **Objective:** {campaign.Goal}

            ## Company Analysis
            Based on our research, {companyName} operates in the {industry} sector and would benefit from 
            targeted messaging around {campaign.Goal.ToLower()}.

            ## Proposed Strategy
            - Focus on industry-specific pain points
            - Leverage {industry} trends and challenges
            - Position our solution as the ideal fit for {companyName}
            
            ## Key Differentiators
            - Industry expertise in {industry}
            - Proven results with similar companies
            - Scalable solution that grows with their business

            ## Next Steps
            Upon approval, we will proceed with content creation and campaign execution.
            """;
    }

    private static List<string> GenerateMockKeyMessages(string industry)
    {
        return industry switch
        {
            "Technology" => ["Innovation-driven solutions", "Scalable technology platform", "Future-ready architecture"],
            "Renewable Energy" => ["Sustainable growth", "Clean energy transition", "Environmental impact"],
            "Financial Services" => ["Secure transactions", "Regulatory compliance", "Digital transformation"],
            _ => ["Industry leadership", "Proven results", "Customer success"]
        };
    }

    private static string GenerateMockTargetAudience(string industry)
    {
        return industry switch
        {
            "Technology" => "CTOs, Engineering Managers, and Technical Decision Makers",
            "Renewable Energy" => "Sustainability Officers, Project Managers, and Executive Leadership",
            "Financial Services" => "Risk Managers, Compliance Officers, and IT Directors",
            _ => "Business Leaders and Decision Makers"
        };
    }
}

/// <summary>
/// Approval history entry
/// </summary>
public record ApprovalHistoryEntry
{
    public ApprovalStatus Action { get; init; }
    public string? Feedback { get; init; }
    public string? ApprovedBy { get; init; }
    public DateTime Timestamp { get; init; }
}
