using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using AgentMarketer.Shared.Contracts;
using AgentMarketer.Shared.DTOs;
namespace AgentMarketer.WebApi.Endpoints;

/// <summary>
/// Campaign management API endpoints
/// </summary>
public static class CampaignEndpoints
{
    public static void MapCampaignEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/campaigns")
            .WithTags("Campaigns")
            .WithOpenApi();

        group.MapPost("/", CreateCampaign)
            .WithName("CreateCampaign")
            .WithSummary("Create a new marketing campaign")
            .WithDescription("Creates a new marketing campaign with the specified goal, audience, and components")
            .Produces<CampaignResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/", GetCampaigns)
            .WithName("GetCampaigns")
            .WithSummary("Get all campaigns")
            .WithDescription("Retrieves a list of all campaigns for the current user")
            .Produces<List<CampaignSummaryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}", GetCampaign)
            .WithName("GetCampaign")
            .WithSummary("Get a specific campaign")
            .WithDescription("Retrieves detailed information about a specific campaign")
            .Produces<CampaignResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id}/plan", CreatePlan)
            .WithName("CreateCampaignPlan")
            .WithSummary("Create execution plan for campaign")
            .WithDescription("Generates an execution plan for the specified campaign")
            .Produces<CreatePlanResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id}/execute", StartExecution)
            .WithName("StartCampaignExecution")
            .WithSummary("Start campaign execution")
            .WithDescription("Begins executing the campaign plan")
            .Produces<ExecutionStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id}/status", GetExecutionStatus)
            .WithName("GetExecutionStatus")
            .WithSummary("Get campaign execution status")
            .WithDescription("Retrieves the current execution status and progress")
            .Produces<ExecutionStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id}/pause", PauseExecution)
            .WithName("PauseCampaignExecution")
            .WithSummary("Pause campaign execution")
            .WithDescription("Pauses the current campaign execution")
            .Produces<ExecutionStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id}/resume", ResumeExecution)
            .WithName("ResumeCampaignExecution")
            .WithSummary("Resume campaign execution")
            .WithDescription("Resumes a paused campaign execution")
            .Produces<ExecutionStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id}/cancel", CancelExecution)
            .WithName("CancelCampaignExecution")
            .WithSummary("Cancel campaign execution")
            .WithDescription("Cancels the current campaign execution")
            .Produces<ExecutionStatusResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);
    }

    /// <summary>
    /// Create a new marketing campaign
    /// </summary>
    /// <param name="request">Campaign creation details</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created campaign details</returns>
    private static async Task<Results<Created<CampaignResponse>, ValidationProblem, ProblemHttpResult>> CreateCampaign(
        CreateCampaignRequest request,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await campaignService.CreateCampaignAsync(request, cancellationToken);
            return TypedResults.Created($"/api/campaigns/{campaign.Id}", campaign);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get all campaigns for the current user
    /// </summary>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of campaign summaries</returns>
    private static async Task<Results<Ok<List<CampaignSummaryResponse>>, ProblemHttpResult>> GetCampaigns(
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var campaigns = await campaignService.GetCampaignsAsync(cancellationToken);
            return TypedResults.Ok(campaigns);
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get detailed information about a specific campaign
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Campaign details or not found</returns>
    private static async Task<Results<Ok<CampaignResponse>, NotFound, ProblemHttpResult>> GetCampaign(
        string id,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var campaign = await campaignService.GetCampaignAsync(id, cancellationToken);
            return campaign is not null 
                ? TypedResults.Ok(campaign) 
                : TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create an execution plan for a campaign
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="request">Plan creation options</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plan creation result</returns>
    private static async Task<Results<Ok<CreatePlanResponse>, NotFound, ProblemHttpResult>> CreatePlan(
        string id,
        CreatePlanRequest request,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await campaignService.CreatePlanAsync(id, request, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException)
        {
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Start executing a campaign
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Execution start result</returns>
    private static async Task<Results<Ok<ExecutionStatusResponse>, NotFound, ProblemHttpResult>> StartExecution(
        string id,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await campaignService.StartExecutionAsync(id, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException)
        {
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get current execution status for a campaign
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current execution status</returns>
    private static async Task<Results<Ok<ExecutionStatusResponse>, NotFound, ProblemHttpResult>> GetExecutionStatus(
        string id,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await campaignService.GetExecutionStatusAsync(id, cancellationToken);
            return status is not null 
                ? TypedResults.Ok(status) 
                : TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Pause campaign execution
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated execution status</returns>
    private static async Task<Results<Ok<ExecutionStatusResponse>, NotFound, ProblemHttpResult>> PauseExecution(
        string id,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await campaignService.PauseExecutionAsync(id, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException)
        {
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Resume campaign execution
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated execution status</returns>
    private static async Task<Results<Ok<ExecutionStatusResponse>, NotFound, ProblemHttpResult>> ResumeExecution(
        string id,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await campaignService.ResumeExecutionAsync(id, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException)
        {
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Cancel campaign execution
    /// </summary>
    /// <param name="id">Campaign identifier</param>
    /// <param name="campaignService">Campaign service</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated execution status</returns>
    private static async Task<Results<Ok<ExecutionStatusResponse>, NotFound, ProblemHttpResult>> CancelExecution(
        string id,
        ICampaignService campaignService,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await campaignService.CancelExecutionAsync(id, cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException)
        {
            return TypedResults.NotFound();
        }
        catch (Exception ex)
        {
            return TypedResults.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
