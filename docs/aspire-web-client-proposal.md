# .NET Aspire Web Client Implementation Proposal

## Overview

This document outlines the approach for implementing a web-based client for the Multi-Agent Campaign Orchestration System using .NET Aspire. The web client will provide a modern, collaborative interface for managing campaigns, reviewing company briefs, and monitoring execution in real-time.

## Current Architecture Analysis

### Existing Components
- **AgentOrchestration** - Core business logic library
- **AgentCmdClient** - Console application interface  
- **CampaignOrchestrationService** - Main orchestration service
- **ContextPersistenceService** - JSON file-based session storage
- **Human-in-the-Loop Workflow** - Company brief review process

### Current Limitations
- Single-user console interface
- File-based storage (not suitable for web/multi-user)
- No real-time collaboration
- Limited scalability
- No web-friendly content preview

## Proposed Aspire Architecture

### New Project Structure
```
AgentMarketerPOC/
├── AgentMarketer.AppHost/           # Aspire orchestration host
├── AgentMarketer.WebApi/            # Minimal API service
├── AgentMarketer.Web/               # Blazor web application
├── AgentMarketer.Cache/             # Redis storage service
├── AgentMarketer.Shared/            # Shared models, DTOs, and contracts
├── AgentOrchestration/              # Core business logic (agents, services, tools)
└── AgentCmdClient/                  # Existing CLI (maintained)
```

### Service Architecture

#### 1. **AgentMarketer.AppHost** (Aspire Orchestration)
- Coordinates all services and dependencies
- Manages service discovery and configuration
- Provides unified development dashboard
- Handles observability and monitoring

#### 2. **AgentMarketer.WebApi** (Minimal API Layer)
- Lightweight minimal API endpoints for campaign operations
- Authentication and authorization middleware
- Background service for campaign execution
- SignalR hubs for real-time updates
- File upload/download capabilities

#### 3. **AgentMarketer.Web** (Blazor Frontend)
- Interactive dashboard and campaign management
- Company brief review interface
- Real-time progress monitoring
- Collaborative approval workflows
- Content preview and editing

#### 4. **AgentMarketer.Cache** (Redis Storage)
- Redis for fast in-memory storage
- JSON serialization for flexible data structures
- Session management with TTL
- Pub/sub for real-time notifications

#### 5. **AgentMarketer.Shared** (Common Models & Contracts)
- Domain models (Campaign, Company, PlanStep, etc.)
- API contracts and DTOs
- Shared enums and constants
- Validation attributes and logic

## Key Features and Benefits

### Enhanced User Experience
1. **Web-Based Interface**
   - Modern, responsive design
   - Mobile-friendly for on-the-go approvals
   - Rich text editing for brief modifications
   - Drag-and-drop file uploads

2. **Real-Time Collaboration**
   - Multiple users can review different company briefs simultaneously
   - Live updates on campaign progress
   - Real-time notifications for approvals needed
   - Chat/comments on brief reviews

3. **Advanced Campaign Management**
   - Visual campaign builder
   - Template library for common campaign types
   - Bulk operations (approve multiple briefs)
   - Campaign cloning and versioning

### Technical Improvements

4. **Scalable Data Storage**
   - Replace JSON files with Redis in-memory storage
   - Support for concurrent users through Redis
   - Session-based data with configurable TTL
   - Built-in pub/sub for real-time updates

5. **Background Processing**
   - Long-running campaign execution as background services
   - Redis-based job queuing for resource-intensive operations
   - Progress tracking and cancellation support
   - Error handling and retry mechanisms

6. **Enterprise Features**
   - User authentication and role-based access
   - Audit trails and compliance reporting
   - API rate limiting and security
   - Multi-tenant support

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
**Goal: Basic web interface with core functionality**

#### Projects to Create:
- `AgentMarketer.AppHost` - Aspire orchestration
- `AgentMarketer.WebApi` - Minimal API endpoints
- `AgentMarketer.Web` - Simple Blazor UI
- `AgentMarketer.Cache` - Redis integration
- `AgentMarketer.Shared` - Common models and DTOs

#### Features:
- Campaign creation and listing
- Basic company brief review interface
- Redis integration for all data storage
- Simple authentication (for POC - no database required)

#### Minimal API Endpoints:
```csharp
// Campaign management
app.MapPost("/api/campaigns", CreateCampaign);
app.MapGet("/api/campaigns", GetCampaigns);
app.MapGet("/api/campaigns/{id}", GetCampaign);
app.MapPost("/api/campaigns/{id}/plan", CreatePlan);
app.MapPost("/api/campaigns/{id}/execute", StartExecution);
app.MapGet("/api/campaigns/{id}/status", GetStatus);

// Company brief management
app.MapGet("/api/campaigns/{id}/briefs", GetCompanyBriefs);
app.MapPost("/api/campaigns/{id}/briefs/{companyId}/approve", ApproveBrief);
app.MapPost("/api/campaigns/{id}/briefs/{companyId}/reject", RejectBrief);
```

### Phase 2: Real-Time Features (Week 3-4)
**Goal: Live collaboration and monitoring**

#### Features:
- SignalR for real-time updates
- Live campaign execution monitoring
- Collaborative brief review
- Push notifications for approvals

#### Technical Components:
- SignalR hubs for campaign updates
- Background services for execution
- WebSocket connections for live data
- Progressive web app (PWA) support

### Phase 3: Advanced Workflows (Week 5-6)
**Goal: Enterprise-ready features**

#### Features:
- Multi-user approval workflows
- Campaign templates and cloning
- Content editing and preview
- Advanced reporting and analytics

#### Technical Components:
- Workflow engine integration
- Rich text editor components
- Export/import capabilities
- Advanced authorization policies

### Phase 4: Production Readiness (Week 7-8)
**Goal: Deployment and optimization**

#### Features:
- Performance optimization
- Security hardening
- Deployment automation
- Monitoring and alerting

## Shared Models Architecture

### Model Migration Strategy
The existing models in `AgentOrchestration/Models/CampaignModels.cs` should be moved to `AgentMarketer.Shared` to enable reuse across projects:

```
AgentMarketer.Shared/
├── Models/
│   ├── Campaign.cs                  # Core domain models
│   ├── Company.cs
│   ├── CampaignPlan.cs
│   ├── PlanStep.cs
│   └── Enums.cs
├── DTOs/
│   ├── CampaignRequests.cs          # API request/response models
│   ├── CampaignResponses.cs
│   ├── ApprovalRequests.cs
│   └── ExecutionResponses.cs
├── Contracts/
│   ├── ICampaignService.cs          # Service interfaces
│   ├── IApprovalService.cs
│   ├── IRedisService.cs
│   └── ICampaignRepository.cs       # Data access interfaces
└── Extensions/
    ├── CampaignExtensions.cs        # Mapping and validation
    ├── RedisExtensions.cs
    └── JsonExtensions.cs            # JSON serialization helpers
```

### Model Updates for Web Support
```csharp
// Enhanced Campaign model with web-friendly properties
public class Campaign
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public CampaignStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "system"; // For web user tracking
    public List<string> CompanyIds { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// Web-specific DTOs
public class CreateCampaignRequest
{
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<string> CompanyNames { get; set; } = new();
}

public class CampaignResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CompanyCount { get; set; }
    public int ApprovedBriefs { get; set; }
    public int PendingApprovals { get; set; }
}
```

### Project References
```
AgentMarketer.WebApi
├── AgentMarketer.Shared
└── AgentOrchestration (for agents and services)

AgentMarketer.Web
├── AgentMarketer.Shared
└── (HTTP client to WebApi)

AgentCmdClient (updated)
├── AgentMarketer.Shared
└── AgentOrchestration

AgentOrchestration (refactored)
├── AgentMarketer.Shared
└── (remove Models, keep Agents/Services/Tools)
```

## Redis Data Design

### Why Redis Instead of Entity Framework?

For this POC, Redis provides significant advantages over a traditional database with Entity Framework:

**Simplicity Benefits:**
- No database schema design or migrations needed
- No complex ORM configuration or DbContext setup
- No SQL knowledge required for development
- JSON serialization matches existing data structures

**Development Speed:**
- Faster project setup and iteration
- No database provisioning or connection string complexity
- Built-in caching eliminates separate cache layer
- Schema changes are just code changes (no migrations)

**POC-Perfect Features:**
- Excellent for mock/temporary data scenarios
- TTL (time-to-live) for automatic session cleanup
- Built-in pub/sub for real-time features
- Redis Insight for easy data inspection during development

**Performance:**
- In-memory storage for ultra-fast reads/writes
- Perfect for session-based workflows
- No query optimization needed
- Natural fit for JSON-heavy data

### Redis Key Patterns
```
# Campaign data
campaigns:{campaignId}                    # Campaign details
campaigns:{campaignId}:plan              # Campaign plan
campaigns:{campaignId}:status            # Execution status
campaigns:{campaignId}:companies         # Target companies
campaigns:{campaignId}:briefs:{companyId} # Company briefs

# Session data
sessions:{sessionId}                     # Session context
sessions:{sessionId}:router              # Router agent state

# User data (for web)
users:{userId}:campaigns                 # User's campaigns
users:{userId}:notifications             # Pending notifications

# Real-time data
campaign:{campaignId}:progress           # Live progress updates
campaign:{campaignId}:approvals          # Pending approvals
```

### Data Structure Examples
```json
// Campaign data
{
  "id": "campaign-123",
  "name": "Q1 Tech Outreach",
  "goal": "Generate leads for tech companies",
  "audience": "SaaS companies with 100+ employees",
  "status": "executing",
  "createdAt": "2025-01-15T10:00:00Z",
  "companies": ["company-1", "company-2"]
}

// Company brief
{
  "companyId": "company-1",
  "companyName": "TechCorp",
  "brief": "Personalized outreach for TechCorp...",
  "status": "pending_approval",
  "generatedAt": "2025-01-15T10:30:00Z",
  "reviewedAt": null,
  "feedback": null
}
```

### Redis Service Example
```csharp
public interface IRedisService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task DeleteAsync(string key);
    Task<List<T>> GetListAsync<T>(string pattern);
    Task PublishAsync<T>(string channel, T message);
    IAsyncEnumerable<T> SubscribeAsync<T>(string pattern);
}

public class RedisService : IRedisService
{
    private readonly IDatabase _database;
    private readonly ISubscriber _subscriber;

    // Simple JSON-based implementation
    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiry);
    }
    
    // ...other methods
}
```

## Blazor Component Architecture

### Page Components
```
/Pages
├── Index.razor                      # Dashboard
├── Campaigns/
│   ├── List.razor                   # Campaign list
│   ├── Create.razor                 # Campaign creation
│   ├── Details.razor                # Campaign details
│   └── Execute.razor                # Execution monitor
├── Reviews/
│   ├── PendingApprovals.razor       # Approval queue
│   ├── CompanyBrief.razor           # Brief review
│   └── BulkApproval.razor           # Bulk operations
└── Content/
    ├── Preview.razor                # Content preview
    └── Editor.razor                 # Content editor
```

### Shared Components
```
/Shared
├── CampaignStatusBadge.razor
├── ProgressIndicator.razor
├── CompanyCard.razor
├── ApprovalWorkflow.razor
├── RealTimeUpdates.razor
└── ContentViewer.razor
```

## SignalR Integration

### Hubs for Real-Time Communication
```csharp
// Campaign execution updates
public class CampaignHub : Hub
{
    public async Task JoinCampaignGroup(string campaignId)
    public async Task LeaveCampaignGroup(string campaignId)
    public async Task SendApprovalUpdate(string campaignId, string companyName, string status)
}

// Real-time notifications
public class NotificationHub : Hub
{
    public async Task JoinUserGroup(string userId)
    public async Task SendNotification(string userId, string message, string type)
}
```

### Client-Side Updates
```javascript
// Blazor JavaScript interop for SignalR
window.signalRFunctions = {
    startConnection: function(hubUrl) { ... },
    joinCampaignGroup: function(campaignId) { ... },
    onCampaignUpdate: function(callback) { ... }
};
```

## Minimal API Design Examples

### Campaign Management
```csharp
// Program.cs - Minimal API setup
var builder = WebApplication.CreateBuilder(args);

// Only Redis - no Entity Framework needed!
builder.Services.AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddSignalR();

var app = builder.Build();

// Campaign endpoints
app.MapPost("/api/campaigns", async (CreateCampaignRequest request, ICampaignService service) =>
{
    var campaign = await service.CreateCampaignAsync(request);
    return Results.Created($"/api/campaigns/{campaign.Id}", campaign);
});

app.MapGet("/api/campaigns/{id}", async (string id, ICampaignService service) =>
{
    var campaign = await service.GetCampaignAsync(id);
    return campaign is not null ? Results.Ok(campaign) : Results.NotFound();
});

app.MapPost("/api/campaigns/{id}/briefs/{companyId}/approve", 
    async (string id, string companyId, ApprovalRequest request, ICampaignService service) =>
{
    await service.ApproveBriefAsync(id, companyId, request.Feedback);
    return Results.Ok();
});
```

### Campaign Service Example
```csharp
public class CampaignService : ICampaignService
{
    private readonly IRedisService _redis;

    public async Task<Campaign> CreateCampaignAsync(CreateCampaignRequest request)
    {
        var campaign = new Campaign
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Goal = request.Goal,
            Audience = request.Audience,
            Status = CampaignStatus.Created,
            CreatedAt = DateTime.UtcNow
        };

        await _redis.SetAsync($"campaigns:{campaign.Id}", campaign);
        return campaign;
    }

    public async Task<Campaign?> GetCampaignAsync(string id)
    {
        return await _redis.GetAsync<Campaign>($"campaigns:{id}");
    }
    
    // No complex EF queries - just simple Redis operations!
}
```

### Real-Time Updates
```csharp
// SignalR Hub for campaign updates
public class CampaignHub : Hub
{
    public async Task JoinCampaignGroup(string campaignId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"campaign-{campaignId}");
    }

    public async Task LeaveCampaignGroup(string campaignId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"campaign-{campaignId}");
    }
}

// Background service publishing updates
public class CampaignExecutionService : BackgroundService
{
    private readonly IHubContext<CampaignHub> _hubContext;
    private readonly IRedisService _redis;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var update in _redis.SubscribeToUpdatesAsync("campaign:*:progress"))
        {
            await _hubContext.Clients.Group($"campaign-{update.CampaignId}")
                .SendAsync("ProgressUpdate", update, stoppingToken);
        }
    }
}
```

## Development Setup with Aspire

### AppHost Configuration
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Redis cache
var redis = builder.AddRedis("cache");

// API Service
var api = builder.AddProject<Projects.AgentMarketer_WebApi>("webapi")
                 .WithReference(redis);

// Web Frontend
var web = builder.AddProject<Projects.AgentMarketer_Web>("web")
                 .WithReference(api);

builder.Build().Run();
```

### Service Registration
```csharp
// WebApi Program.cs
builder.Services.AddStackExchangeRedis(builder.Configuration.GetConnectionString("Redis"));

// Core services from AgentOrchestration
builder.Services.AddScoped<CampaignOrchestrationService>();

// Redis-based persistence (replaces ContextPersistenceService)
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<ICampaignRepository, RedisCampaignRepository>();

// Web-specific services
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

// Background services
builder.Services.AddHostedService<CampaignExecutionService>();

builder.Services.AddSignalR();
```

## Deployment Strategy

### Development Environment
- Aspire dashboard for service orchestration
- Redis for local development (via Docker)
- Hot reload for rapid development
- Integrated debugging across services

### Production Environment
- Container deployment with Docker
- Azure Cache for Redis
- Azure SignalR Service for scale-out
- Application Insights for monitoring

## Migration Plan

### Data Migration
1. Create Redis service and connection configuration
2. Build data migration tool from JSON to Redis
3. Preserve existing campaign sessions in Redis format
4. Maintain backward compatibility with existing CLI

### Gradual Rollout
1. Keep CLI client functional (update to use Redis)
2. Run web and CLI in parallel
3. Gradual user migration to web interface
4. Eventually deprecate CLI (optional)

## Benefits Summary

### For Users
- **Better Experience**: Modern web interface vs. command line
- **Collaboration**: Multiple team members can work simultaneously
- **Mobile Access**: Review briefs on mobile devices
- **Rich Content**: Better visualization of generated content

### For Development
- **Maintainability**: Clean separation of concerns
- **Scalability**: Can handle multiple concurrent users
- **Observability**: Built-in monitoring and logging
- **Testing**: Better unit and integration testing capabilities

### For Business
- **Productivity**: Faster approval workflows
- **Compliance**: Better audit trails and governance
- **Integration**: API-first design enables integrations
- **Growth**: Platform can scale with business needs

## Next Steps

1. **Review and Approve Architecture**: Validate technical decisions
2. **Set Up Development Environment**: Create Aspire projects with Redis
3. **Implement Redis Data Layer**: Simple JSON-based storage without EF complexity
4. **Phase 1 Implementation**: Start with basic web API and UI
5. **Iterative Development**: Build and test incrementally

## Architecture Benefits Summary

This Redis-based approach provides the perfect balance for a POC:

**Simplicity:** 
- No database design or migrations
- No ORM complexity or configuration
- Simple JSON serialization matches existing code patterns

**Speed:**
- Faster development setup and iteration
- In-memory performance for all operations
- Built-in real-time capabilities with pub/sub

**POC-Perfect:**
- Easy to demo and show progress
- Flexible schema-less data structure
- Simple deployment (just Redis container)
- Natural fit for session-based workflows

This approach leverages .NET Aspire's strengths while keeping the data layer simple and focused on rapid POC development, avoiding the overhead of Entity Framework while preserving all existing functionality.
