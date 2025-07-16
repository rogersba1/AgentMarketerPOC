using AgentMarketer.WebApi.Services;
using AgentOrchestration.Services;
using AgentOrchestration.Services.Modern;
using AgentOrchestration.Tools;
using Microsoft.SemanticKernel;

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

// Configure Semantic Kernel with Azure AI Inference (for Azure Foundry)
builder.Services.AddSingleton<Kernel>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    // Try Azure AI Foundry endpoint first
    var foundryEndpoint = configuration["AzureFoundry:Endpoint"];
    var foundryApiKey = configuration["AzureFoundry:ApiKey"];
    var foundryModel = configuration["AzureFoundry:ModelName"];

    if (!string.IsNullOrEmpty(foundryEndpoint) && !string.IsNullOrEmpty(foundryApiKey) && !string.IsNullOrEmpty(foundryModel))
    {
        Console.WriteLine($"Using Azure AI Foundry service at {foundryEndpoint} with model {foundryModel}");
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: foundryModel,
            apiKey: foundryApiKey,
            endpoint: new Uri(foundryEndpoint));
#pragma warning restore SKEXP0010
    }
    else
    {
        // Fallback to OpenAI
        var openAIApiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAIApiKey))
        {
            Console.WriteLine("Using OpenAI service");
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: "gpt-4o",
                apiKey: openAIApiKey);
        }
        else
        {
            // Try Azure OpenAI as fallback
            var azureEndpoint = configuration["AzureOpenAI:Endpoint"];
            var azureApiKey = configuration["AzureOpenAI:ApiKey"];
            var deploymentName = configuration["AzureOpenAI:DeploymentName"];

            if (!string.IsNullOrEmpty(azureEndpoint) && !string.IsNullOrEmpty(azureApiKey) && !string.IsNullOrEmpty(deploymentName))
            {
                Console.WriteLine("Using Azure OpenAI service");
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentName,
                    endpoint: azureEndpoint,
                    apiKey: azureApiKey);
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

// Make the Program class accessible for testing
public partial class Program { }
