using AgentMarketer.Shared.Contracts;
using AgentMarketer.Shared.DTOs;
using System.Text.Json;

namespace AgentMarketer.Web.Services;

/// <summary>
/// HTTP client implementation of IApprovalService for the Blazor web client
/// </summary>
public class ApprovalHttpService : IApprovalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApprovalHttpService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApprovalHttpService(IHttpClientFactory httpClientFactory, ILogger<ApprovalHttpService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AgentMarketerAPI");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<CompanyBriefResponse>> GetCompanyBriefsAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting company briefs for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.GetAsync($"/api/approvals/{campaignId}/briefs", cancellationToken);
            response.EnsureSuccessStatusCode();

            var briefs = await response.Content.ReadFromJsonAsync<List<CompanyBriefResponse>>(_jsonOptions, cancellationToken);
            return briefs ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting company briefs for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company briefs for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<List<CompanyBriefResponse>> GetPendingBriefsAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting pending briefs for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.GetAsync($"/api/approvals/{campaignId}/briefs/pending", cancellationToken);
            response.EnsureSuccessStatusCode();

            var briefs = await response.Content.ReadFromJsonAsync<List<CompanyBriefResponse>>(_jsonOptions, cancellationToken);
            return briefs ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting pending briefs for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending briefs for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<CompanyBriefResponse?> GetCompanyBriefAsync(string campaignId, string companyId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting company brief for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);

        try
        {
            var response = await _httpClient.GetAsync($"/api/approvals/{campaignId}/briefs/{companyId}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();

            var brief = await response.Content.ReadFromJsonAsync<CompanyBriefResponse>(_jsonOptions, cancellationToken);
            return brief;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting company brief for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company brief for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            throw;
        }
    }

    public async Task<ApprovalActionResponse> ProcessApprovalAsync(string campaignId, string companyId, ApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing approval for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/approvals/{campaignId}/briefs/{companyId}/approve", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ApprovalActionResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize approval response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error processing approval for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approval for campaign: {CampaignId}, company: {CompanyId}", campaignId, companyId);
            throw;
        }
    }

    public async Task<BulkApprovalResponse> ProcessBulkApprovalAsync(BulkApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing bulk approval for campaign: {CampaignId}", request.CampaignId);

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/approvals/bulk", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BulkApprovalResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize bulk approval response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error processing bulk approval for campaign: {CampaignId}", request.CampaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk approval for campaign: {CampaignId}", request.CampaignId);
            throw;
        }
    }

    public async Task<List<PendingApprovalsSummary>> GetPendingApprovalsSummaryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting pending approvals summary");

        try
        {
            var response = await _httpClient.GetAsync("/api/approvals/pending", cancellationToken);
            response.EnsureSuccessStatusCode();

            var summaries = await response.Content.ReadFromJsonAsync<List<PendingApprovalsSummary>>(_jsonOptions, cancellationToken);
            return summaries ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting pending approvals summary");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals summary");
            throw;
        }
    }

    public async Task<PendingApprovalsSummary?> GetCampaignPendingApprovalsAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting pending approvals for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.GetAsync($"/api/approvals/{campaignId}/pending", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();

            var summary = await response.Content.ReadFromJsonAsync<PendingApprovalsSummary>(_jsonOptions, cancellationToken);
            return summary;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting pending approvals for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending approvals for campaign: {CampaignId}", campaignId);
            throw;
        }
    }
}
