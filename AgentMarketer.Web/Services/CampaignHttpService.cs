using AgentMarketer.Shared.Contracts;
using AgentMarketer.Shared.DTOs;
using System.Text.Json;

namespace AgentMarketer.Web.Services;

/// <summary>
/// HTTP client implementation of ICampaignService for the Blazor web client
/// </summary>
public class CampaignHttpService : ICampaignService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CampaignHttpService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CampaignHttpService(IHttpClientFactory httpClientFactory, ILogger<CampaignHttpService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AgentMarketerAPI");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<CampaignResponse> CreateCampaignAsync(CreateCampaignRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating campaign: {CampaignName}", request.Name);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/campaigns", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CampaignResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize campaign response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating campaign: {CampaignName}", request.Name);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign: {CampaignName}", request.Name);
            throw;
        }
    }

    public async Task<List<CampaignSummaryResponse>> GetCampaignsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving all campaigns");

        try
        {
            var response = await _httpClient.GetAsync("/api/campaigns", cancellationToken);
            response.EnsureSuccessStatusCode();

            var campaigns = await response.Content.ReadFromJsonAsync<List<CampaignSummaryResponse>>(_jsonOptions, cancellationToken);
            return campaigns ?? [];
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error retrieving campaigns");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaigns");
            throw;
        }
    }

    public async Task<CampaignResponse?> GetCampaignAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.GetAsync($"/api/campaigns/{campaignId}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();

            var campaign = await response.Content.ReadFromJsonAsync<CampaignResponse>(_jsonOptions, cancellationToken);
            return campaign;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error retrieving campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<CreatePlanResponse> CreatePlanAsync(string campaignId, CreatePlanRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating plan for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/campaigns/{campaignId}/plan", request, _jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CreatePlanResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize plan response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating plan for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<ExecutionStatusResponse> StartExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting execution for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/execution", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExecutionStatusResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize execution response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error starting execution for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting execution for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<ExecutionStatusResponse?> GetExecutionStatusAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting execution status for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.GetAsync($"/api/campaigns/{campaignId}/execution", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();

            var status = await response.Content.ReadFromJsonAsync<ExecutionStatusResponse>(_jsonOptions, cancellationToken);
            return status;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error getting execution status for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting execution status for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<ExecutionStatusResponse> PauseExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pausing execution for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/execution/pause", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExecutionStatusResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize execution response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error pausing execution for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing execution for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<ExecutionStatusResponse> ResumeExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming execution for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/execution/resume", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExecutionStatusResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize execution response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error resuming execution for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming execution for campaign: {CampaignId}", campaignId);
            throw;
        }
    }

    public async Task<ExecutionStatusResponse> CancelExecutionAsync(string campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling execution for campaign: {CampaignId}", campaignId);

        try
        {
            var response = await _httpClient.PostAsync($"/api/campaigns/{campaignId}/execution/cancel", null, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExecutionStatusResponse>(_jsonOptions, cancellationToken);
            return result ?? throw new InvalidOperationException("Failed to deserialize execution response");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error cancelling execution for campaign: {CampaignId}", campaignId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling execution for campaign: {CampaignId}", campaignId);
            throw;
        }
    }
}
