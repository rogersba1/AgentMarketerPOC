using AgentMarketer.WebApi.Services;
using AgentOrchestration.Services;
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

// Configure Semantic Kernel
builder.Services.AddScoped<Kernel>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var kernelBuilder = Kernel.CreateBuilder();

    // Try to get Azure OpenAI configuration
    var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
    var azureOpenAIApiKey = configuration["AzureOpenAI:ApiKey"];
    var azureOpenAIDeployment = configuration["AzureOpenAI:DeploymentName"];

    if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIApiKey))
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: azureOpenAIDeployment ?? "gpt-4",
            endpoint: azureOpenAIEndpoint,
            apiKey: azureOpenAIApiKey);
    }
    else
    {
        // Fallback to OpenAI
        var openAIApiKey = configuration["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(openAIApiKey))
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: "gpt-4",
                apiKey: openAIApiKey);
        }
        else
        {
            // For development/testing, create a basic kernel without AI service
            Console.WriteLine("Warning: No AI service configured. Using basic kernel for testing.");
        }
    }

    return kernelBuilder.Build();
});

// Register data services
builder.Services.AddScoped<MockCompanyDataService>();

// Register tools
builder.Services.AddScoped<ContentGenerationTools>();

// Register your existing orchestration services
builder.Services.AddScoped<CampaignOrchestrationService>();
builder.Services.AddScoped<ContextPersistenceService>();
builder.Services.AddScoped<CampaignParsingService>();

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
