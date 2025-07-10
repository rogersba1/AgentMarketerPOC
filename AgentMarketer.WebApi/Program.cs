using AgentMarketer.WebApi.Endpoints;
using AgentMarketer.WebApi.Hubs;
using AgentMarketer.WebApi.Services;
using AgentOrchestration.Services;
using AgentMarketer.Shared.Contracts;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.AddConsole();

// Add Redis connection string from configuration
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => 
{
    var connection = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString);
    return connection;
});

// Add OpenAPI documentation
builder.Services.AddOpenApi();

// Register core orchestration services
builder.Services.AddSingleton<CampaignOrchestrationService>();

// Register Redis and web services
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// Map API endpoints
app.MapCampaignEndpoints();
app.MapApprovalEndpoints();
app.MapChatEndpoints();

// Map SignalR hubs
app.MapHub<CampaignHub>("/chathub");

app.Run();
