var builder = DistributedApplication.CreateBuilder(args);

// Add Redis cache for data storage and real-time features
var redis = builder.AddRedis("cache")
    .WithDataVolume()
    .WithRedisInsight(); // Adds Redis Insight for easy data inspection during development

// Add Web API service with Redis reference
var webApi = builder.AddProject<Projects.AgentMarketer_WebApi>("webapi")
    .WithReference(redis)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

// Add Blazor Web service with API reference
var web = builder.AddProject<Projects.AgentMarketer_Web>("web")
    .WithReference(webApi)
    .WithExternalHttpEndpoints();

var app = builder.Build();

app.Run();
