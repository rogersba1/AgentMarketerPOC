# Quick Start: Aspire Web Client Implementation (Redis-Based)

This guide provides step-by-step instructions to implement the .NET Aspire web client for the Multi-Agent Campaign Orchestration System using Redis for storage.

## Prerequisites

- .NET 8 SDK or later
- Visual Studio 2022 17.9+ or VS Code with C# extension
- .NET Aspire workload installed
- Docker Desktop (for Redis container)

### Install .NET Aspire Workload
```bash
dotnet workload update
dotnet workload install aspire
```

## Step 1: Create Aspire Project Structure

### 1.1 Create Aspire App Host
```bash
# From the root AgentMarketerPOC directory
dotnet new aspire-apphost -n AgentMarketer.AppHost
```

### 1.2 Create Web API Project
```bash
dotnet new webapi -n AgentMarketer.WebApi
cd AgentMarketer.WebApi
dotnet add package Aspire.StackExchangeRedis
dotnet add package Microsoft.AspNetCore.SignalR
dotnet add package StackExchange.Redis
cd ..
```

### 1.3 Create Blazor Web Project
```bash
dotnet new blazor -n AgentMarketer.Web --interactivity Server
cd AgentMarketer.Web
dotnet add package Microsoft.AspNetCore.SignalR.Client
cd ..
```

### 1.4 Create Shared Library
```bash
dotnet new classlib -n AgentMarketer.Shared
cd AgentMarketer.Shared
dotnet add package System.ComponentModel.DataAnnotations
cd ..
```

### 1.5 Update Solution File
```bash
dotnet sln add AgentMarketer.AppHost
dotnet sln add AgentMarketer.WebApi
dotnet sln add AgentMarketer.Web
dotnet sln add AgentMarketer.Shared
```

## Step 2: Configure Project References

### 2.1 Add References to AppHost
```bash
cd AgentMarketer.AppHost
dotnet add reference ../AgentMarketer.WebApi
dotnet add reference ../AgentMarketer.Web
cd ..
```

### 2.2 Add References to WebApi
```bash
cd AgentMarketer.WebApi
dotnet add reference ../AgentOrchestration
dotnet add reference ../AgentMarketer.Shared
cd ..
```

### 2.3 Add References to Web
```bash
cd AgentMarketer.Web
dotnet add reference ../AgentMarketer.Shared
cd ..
```

## Step 3: Configure Aspire App Host

### 3.1 Update AppHost Program.cs
```csharp
// AgentMarketer.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache
var redis = builder.AddRedis("cache");

// Add Web API service
var api = builder.AddProject<Projects.AgentMarketer_WebApi>("webapi")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Add Blazor Web service
var web = builder.AddProject<Projects.AgentMarketer_Web>("web")
    .WithReference(api)
    .WithExternalHttpEndpoints();

var app = builder.Build();

app.Run();
```

### 3.2 Update AppHost Project File
```xml
<!-- AgentMarketer.AppHost/AgentMarketer.AppHost.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsAspireHost>true</IsAspireHost>
    <UserSecretsId>aspire-agentmarketer-apphost</UserSecretsId>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Aspire.Hosting.AppHost" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\AgentMarketer.WebApi\AgentMarketer.WebApi.csproj" />
    <ProjectReference Include="..\AgentMarketer.Web\AgentMarketer.Web.csproj" />
  </ItemGroup>

</Project>
```

## Step 4: Create Shared Data Models

### 4.1 Campaign DTOs
```csharp
// AgentMarketer.Shared/Models/CampaignDto.cs
using System.ComponentModel.DataAnnotations;

namespace AgentMarketer.Shared.Models;

public class CreateCampaignRequest
{
    [Required]
    public string Goal { get; set; } = string.Empty;
    
    [Required]
    public string Audience { get; set; } = string.Empty;
    
    public List<string> Components { get; set; } = new();
}

public class CampaignResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<string> Components { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
}

public class CompanyBriefResponse
{
    public string CompanyName { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string ApprovalStatus { get; set; } = string.Empty;
    public string? HumanFeedback { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class ApprovalRequest
{
    public string Feedback { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true;
    public bool IsModified { get; set; } = false;
}
```

## Step 5: Implement Web API

### 5.1 Create Database Context
```csharp
// AgentMarketer.WebApi/Data/AgentMarketerDbContext.cs
using Microsoft.EntityFrameworkCore;
using AgentOrchestration.Models;

namespace AgentMarketer.WebApi.Data;

public class AgentMarketerDbContext : DbContext
{
    public AgentMarketerDbContext(DbContextOptions<AgentMarketerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CampaignPlan> CampaignPlans { get; set; }
    public DbSet<PlanStep> PlanSteps { get; set; }
    public DbSet<CompanyProfile> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure entity relationships and constraints
        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Components)
                  .HasConversion(
                      v => string.Join(',', v),
                      v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        modelBuilder.Entity<PlanStep>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Parameters)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new());
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

### 5.2 Create Campaigns Controller
```csharp
// AgentMarketer.WebApi/Controllers/CampaignsController.cs
using Microsoft.AspNetCore.Mvc;
using AgentMarketer.Shared.Models;
using AgentOrchestration.Services;

namespace AgentMarketer.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CampaignsController : ControllerBase
{
    private readonly CampaignOrchestrationService _orchestrationService;

    public CampaignsController(CampaignOrchestrationService orchestrationService)
    {
        _orchestrationService = orchestrationService;
    }

    [HttpPost]
    public async Task<ActionResult<CampaignResponse>> CreateCampaign(CreateCampaignRequest request)
    {
        var naturalLanguageInput = $"Create a marketing campaign with goal: {request.Goal}, targeting audience: {request.Audience}, including components: {string.Join(", ", request.Components)}";
        
        var (sessionId, response) = await _orchestrationService.StartNewCampaignFromNaturalLanguageAsync(naturalLanguageInput);
        
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest(response);
        }

        var session = await _orchestrationService.GetSessionAsync(sessionId);
        
        return Ok(new CampaignResponse
        {
            Id = session.Id,
            Name = session.Campaign.Name,
            Goal = session.Campaign.Goal,
            Audience = session.Campaign.Audience,
            Components = session.Campaign.Components,
            Status = session.Campaign.Status.ToString(),
            CreatedAt = session.Campaign.CreatedAt,
            ExecutedAt = session.Campaign.ExecutedAt
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<CampaignResponse>>> GetCampaigns()
    {
        var campaigns = await _orchestrationService.ListActiveCampaignsAsync();
        // This would need to be adapted to return proper DTOs
        return Ok(new List<CampaignResponse>());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CampaignResponse>> GetCampaign(string id)
    {
        try
        {
            var session = await _orchestrationService.GetSessionAsync(id);
            
            return Ok(new CampaignResponse
            {
                Id = session.Id,
                Name = session.Campaign.Name,
                Goal = session.Campaign.Goal,
                Audience = session.Campaign.Audience,
                Components = session.Campaign.Components,
                Status = session.Campaign.Status.ToString(),
                CreatedAt = session.Campaign.CreatedAt,
                ExecutedAt = session.Campaign.ExecutedAt
            });
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/plan")]
    public async Task<ActionResult<string>> CreatePlan(string id)
    {
        try
        {
            var result = await _orchestrationService.CreateCampaignPlanAsync(id);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/execute")]
    public async Task<ActionResult<string>> ExecuteCampaign(string id)
    {
        try
        {
            var result = await _orchestrationService.ExecuteCampaignAsync(id);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/briefs")]
    public async Task<ActionResult<List<CompanyBriefResponse>>> GetCompanyBriefs(string id)
    {
        try
        {
            var session = await _orchestrationService.GetSessionAsync(id);
            var router = _orchestrationService.GetRouterAgent();
            var pendingApprovals = await router.ListPendingApprovals(session);
            
            // This would need to be properly parsed and returned as structured data
            // For now, return empty list as placeholder
            return Ok(new List<CompanyBriefResponse>());
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/briefs/{companyName}/approve")]
    public async Task<ActionResult<string>> ApproveBrief(string id, string companyName, ApprovalRequest request)
    {
        try
        {
            var result = await _orchestrationService.ApproveCompanyBriefAsync(
                id, companyName, request.Feedback, request.IsModified, !request.IsApproved);
            return Ok(result);
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }
}
```

### 5.3 Update WebApi Program.cs
```csharp
// AgentMarketer.WebApi/Program.cs
using AgentMarketer.WebApi.Data;
using AgentOrchestration.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.AddServiceDefaults();

// Add database
builder.Services.AddDbContext<AgentMarketerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("agentmarketer")));

// Add campaign orchestration services
builder.Services.AddSingleton<CampaignOrchestrationService>();

// Add API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for Blazor client
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.MapControllers();

app.Run();
```

## Step 6: Create Basic Blazor UI

### 6.1 Update Web Program.cs
```csharp
// AgentMarketer.Web/Program.cs
using AgentMarketer.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HTTP client for API calls
builder.Services.AddHttpClient("WebApi", client =>
{
    client.BaseAddress = new Uri("https+http://webapi");
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

### 6.2 Create Campaign Management Page
```razor
@* AgentMarketer.Web/Components/Pages/Campaigns.razor *@
@page "/campaigns"
@using AgentMarketer.Shared.Models
@inject IHttpClientFactory HttpClientFactory
@rendermode InteractiveServer

<PageTitle>Campaign Management</PageTitle>

<h1>Campaign Management</h1>

<div class="row">
    <div class="col-md-6">
        <h3>Create New Campaign</h3>
        <EditForm Model="@newCampaign" OnValidSubmit="@CreateCampaign">
            <DataAnnotationsValidator />
            <ValidationSummary />
            
            <div class="mb-3">
                <label for="goal" class="form-label">Campaign Goal</label>
                <InputText id="goal" class="form-control" @bind-Value="newCampaign.Goal" />
            </div>
            
            <div class="mb-3">
                <label for="audience" class="form-label">Target Audience</label>
                <InputText id="audience" class="form-control" @bind-Value="newCampaign.Audience" />
            </div>
            
            <div class="mb-3">
                <label for="components" class="form-label">Components (comma-separated)</label>
                <InputText id="components" class="form-control" @bind-Value="componentsText" />
            </div>
            
            <button type="submit" class="btn btn-primary" disabled="@isCreating">
                @if (isCreating)
                {
                    <span class="spinner-border spinner-border-sm me-2"></span>
                }
                Create Campaign
            </button>
        </EditForm>
    </div>
    
    <div class="col-md-6">
        <h3>Active Campaigns</h3>
        @if (campaigns.Any())
        {
            <div class="list-group">
                @foreach (var campaign in campaigns)
                {
                    <div class="list-group-item">
                        <div class="d-flex w-100 justify-content-between">
                            <h5 class="mb-1">@campaign.Goal</h5>
                            <small class="text-muted">@campaign.Status</small>
                        </div>
                        <p class="mb-1">@campaign.Audience</p>
                        <small class="text-muted">Created: @campaign.CreatedAt.ToString("yyyy-MM-dd HH:mm")</small>
                        <div class="mt-2">
                            <button class="btn btn-sm btn-outline-primary me-2" @onclick="() => NavigateToCampaign(campaign.Id)">
                                View Details
                            </button>
                            <button class="btn btn-sm btn-outline-success" @onclick="() => ExecuteCampaign(campaign.Id)">
                                Execute
                            </button>
                        </div>
                    </div>
                }
            </div>
        }
        else
        {
            <p class="text-muted">No campaigns found. Create your first campaign!</p>
        }
    </div>
</div>

@code {
    private CreateCampaignRequest newCampaign = new();
    private string componentsText = "landing page, email, linkedin post";
    private List<CampaignResponse> campaigns = new();
    private bool isCreating = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadCampaigns();
    }

    private async Task CreateCampaign()
    {
        isCreating = true;
        
        try
        {
            newCampaign.Components = componentsText.Split(',')
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            var client = HttpClientFactory.CreateClient("WebApi");
            var response = await client.PostAsJsonAsync("/api/campaigns", newCampaign);
            
            if (response.IsSuccessStatusCode)
            {
                newCampaign = new();
                componentsText = "landing page, email, linkedin post";
                await LoadCampaigns();
            }
        }
        finally
        {
            isCreating = false;
        }
    }

    private async Task LoadCampaigns()
    {
        try
        {
            var client = HttpClientFactory.CreateClient("WebApi");
            var response = await client.GetFromJsonAsync<List<CampaignResponse>>("/api/campaigns");
            campaigns = response ?? new List<CampaignResponse>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading campaigns: {ex.Message}");
        }
    }

    private void NavigateToCampaign(string campaignId)
    {
        // Navigation logic - would be implemented with NavigationManager
    }

    private async Task ExecuteCampaign(string campaignId)
    {
        try
        {
            var client = HttpClientFactory.CreateClient("WebApi");
            await client.PostAsync($"/api/campaigns/{campaignId}/execute", null);
            await LoadCampaigns();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing campaign: {ex.Message}");
        }
    }
}
```

## Step 7: Run the Application

### 7.1 Build and Run
```bash
# Build the entire solution
dotnet build

# Run the Aspire AppHost
cd AgentMarketer.AppHost
dotnet run
```

### 7.2 Access the Application
- **Aspire Dashboard**: https://localhost:17294 (or shown in console)
- **Web API**: Available through Aspire service discovery
- **Blazor Web**: Available through Aspire service discovery
- **Swagger UI**: Available at the WebApi endpoint + /swagger

## Next Steps

1. **Database Migrations**: Create and run EF Core migrations
2. **Authentication**: Implement user authentication and authorization
3. **SignalR**: Add real-time updates for campaign execution
4. **Company Brief Review**: Implement the detailed review interface
5. **Error Handling**: Add comprehensive error handling and logging
6. **Testing**: Create unit and integration tests

This foundation provides the basic structure for migrating the console-based human-in-the-loop workflow to a modern web-based interface using .NET Aspire.
