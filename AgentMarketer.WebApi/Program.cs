using AgentMarketer.WebApi.Services;
using AgentOrchestration.Services;
using AgentOrchestration.Services.Modern;
using AgentOrchestration.Tools;
using Microsoft.SemanticKernel;
using Microsoft.Extensions.AI;
using Microsoft.AI.Foundry.Local;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { "https://localhost:7002", "http://localhost:5002" };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Configure Semantic Kernel with Azure Local Foundry
builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    // Try to get Azure Local Foundry configuration
    var foundryModelAlias = configuration["AzureLocalFoundry:ModelName"] ?? "phi-3.5-mini";

    try
    {
        // Start the Foundry Local service if not already running
        var manager = FoundryLocalManager.StartModelAsync(aliasOrModelId: foundryModelAlias).GetAwaiter().GetResult();
        
        var model = manager.GetModelInfoAsync(aliasOrModelId: foundryModelAlias).GetAwaiter().GetResult();
        
        Console.WriteLine($"Foundry Local service started successfully with model: {model?.ModelId ?? foundryModelAlias}");
        Console.WriteLine($"Endpoint: {manager.Endpoint}");
        
        // Use the manager's endpoint and API key for Semantic Kernel
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: foundryModelAlias,
            apiKey: manager.ApiKey,
            endpoint: manager.Endpoint);
#pragma warning restore SKEXP0010
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to start Foundry Local service: {ex.Message}");
        
        // Fallback to OpenAI if Foundry Local fails
        var openAIApiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAIApiKey))
        {
            Console.WriteLine("Falling back to OpenAI service");
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: "gpt-4",
                apiKey: openAIApiKey);
        }
        else
        {
            // For development/testing, add a mock chat completion service
            Console.WriteLine("Warning: No AI service configured. Using mock chat completion service for testing.");
            kernelBuilder.Services.AddKeyedSingleton<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>(
                "default", 
                (serviceProvider, key) => new AgentMarketer.WebApi.Services.MockChatCompletionService());
        }
    }

    return kernelBuilder.Build();
});

// Register data services
builder.Services.AddScoped<MockCompanyDataService>();

// Register tools
builder.Services.AddScoped<ContentGenerationTools>();

// Register your existing orchestration services
builder.Services.AddScoped<ContextPersistenceService>();

// Register the new sequential orchestration service
builder.Services.AddScoped<SequentialCampaignOrchestrationService>();

// Register the new chat bridge service
builder.Services.AddScoped<ChatOrchestrationBridge>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowLocalhost");

app.UseAuthorization();

app.MapControllers();

app.Run();
