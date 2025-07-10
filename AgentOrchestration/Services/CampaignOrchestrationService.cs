using Microsoft.SemanticKernel;
using Microsoft.Extensions.Configuration;
using AgentOrchestration.Models;
using AgentOrchestration.Agents;
using AgentOrchestration.Tools;
using AgentOrchestration.Services;
using System;
using System.Threading.Tasks;

namespace AgentOrchestration.Services
{
    /// <summary>
    /// Main orchestration service that coordinates all agents and manages campaign execution
    /// </summary>
    public class CampaignOrchestrationService
    {
        private readonly Kernel _kernel;
        private readonly PlannerAgent _plannerAgent;
        private readonly ResearcherAgent _researcherAgent;
        private readonly RouterAgent _routerAgent;
        private readonly ContentGenerationTools _contentTools;
        private readonly ContextPersistenceService _persistenceService;
        private readonly CampaignParsingService _parsingService;
        private readonly MockCompanyDataService _companyDataService;

        public CampaignOrchestrationService(IConfiguration configuration)
        {
            _kernel = CreateKernel(configuration);
            _companyDataService = new MockCompanyDataService();
            _contentTools = new ContentGenerationTools(_kernel, _companyDataService);
            _researcherAgent = new ResearcherAgent(_kernel);
            _plannerAgent = new PlannerAgent(_kernel);
            _routerAgent = new RouterAgent(_kernel, _researcherAgent, _contentTools);
            _persistenceService = new ContextPersistenceService();
            _parsingService = new CampaignParsingService(_kernel);

            // Initialize company data
            Task.Run(async () => await _companyDataService.LoadCompanyDataAsync());

            // Register content generation tools with the kernel
            _kernel.Plugins.AddFromObject(_contentTools);
        }

        /// <summary>
        /// Starts a new campaign session from natural language input
        /// </summary>
        public async Task<(string sessionId, string response)> StartNewCampaignFromNaturalLanguageAsync(string naturalLanguageInput)
        {
            try
            {
                // Parse the natural language input
                var parsedCampaign = await _parsingService.ParseCampaignInputAsync(naturalLanguageInput);

                var session = new CampaignSession
                {
                    Campaign = new Campaign
                    {
                        Goal = parsedCampaign.Goal,
                        Audience = parsedCampaign.Audience,
                        Components = parsedCampaign.Components,
                        Status = CampaignStatus.Draft
                    }
                };

                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: New campaign session started from natural language");
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Original input: {naturalLanguageInput}");
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Parsed goal: {parsedCampaign.Goal}");
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Parsed audience: {parsedCampaign.Audience}");
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Parsed components: {string.Join(", ", parsedCampaign.Components)}");

                await _persistenceService.SaveSessionAsync(session);

                var response = $@"
🚀 New Campaign Session Started!

Session ID: {session.Id}

📝 **Parsed from your request:**
Original Input: ""{naturalLanguageInput}""

🎯 **Campaign Goal:** {parsedCampaign.Goal}
👥 **Target Audience:** {parsedCampaign.Audience}  
📋 **Components:** {string.Join(", ", parsedCampaign.Components)}

Your campaign is ready for planning. The next step is to create an execution plan.
";

                return (session.Id, response);
            }
            catch (Exception ex)
            {
                return (string.Empty, $"Error starting campaign: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts a new campaign session (legacy method for backward compatibility)
        /// </summary>
        public async Task<(string sessionId, string response)> StartNewCampaignAsync(string goal, string audience, string[] components)
        {
            try
            {
                var session = new CampaignSession
                {
                    Campaign = new Campaign
                    {
                        Goal = goal,
                        Audience = audience,
                        Components = components.ToList(),
                        Status = CampaignStatus.Draft
                    }
                };

                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: New campaign session started");
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Goal: {goal}");
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Audience: {audience}");
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Components: {string.Join(", ", components)}");

                await _persistenceService.SaveSessionAsync(session);

                var response = $@"
🚀 New Campaign Session Started!

Session ID: {session.Id}
Campaign Goal: {goal}
Target Audience: {audience}
Components: {string.Join(", ", components)}

Your campaign is ready for planning. The next step is to create an execution plan.
";

                return (session.Id, response);
            }
            catch (Exception ex)
            {
                return (string.Empty, $"Error starting campaign: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a campaign execution plan
        /// </summary>
        public async Task<string> CreateCampaignPlanAsync(string sessionId, string additionalInstructions = "")
        {
            try
            {
                var (success, session, message) = await _persistenceService.ResumeSessionAsync(sessionId);
                if (!success || session == null)
                {
                    return $"Error: {message}";
                }

                var planningInput = string.IsNullOrEmpty(additionalInstructions) 
                    ? "Create a comprehensive campaign execution plan"
                    : additionalInstructions;

                var response = await _plannerAgent.ProcessAsync(planningInput, session);
                
                await _persistenceService.SaveSessionAsync(session);

                return response;
            }
            catch (Exception ex)
            {
                return $"Error creating campaign plan: {ex.Message}";
            }
        }

        /// <summary>
        /// Executes the campaign plan
        /// </summary>
        public async Task<string> ExecuteCampaignAsync(string sessionId)
        {
            try
            {
                var (success, session, message) = await _persistenceService.ResumeSessionAsync(sessionId);
                if (!success || session == null)
                {
                    return $"Error: {message}";
                }

                if (session.Plan == null)
                {
                    return "No campaign plan found. Please create a plan first using CreateCampaignPlanAsync.";
                }

                var response = await _routerAgent.ExecutePlan(session);
                
                await _persistenceService.SaveSessionAsync(session);

                return response;
            }
            catch (Exception ex)
            {
                return $"Error executing campaign: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets the current status of a campaign
        /// </summary>
        public async Task<string> GetCampaignStatusAsync(string sessionId)
        {
            try
            {
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {
                    return $"Session {sessionId} not found";
                }

                return await _routerAgent.GetExecutionStatus(session);
            }
            catch (Exception ex)
            {
                return $"Error getting campaign status: {ex.Message}";
            }
        }

        /// <summary>
        /// Resumes a campaign from a previous session
        /// </summary>
        public async Task<string> ResumeCampaignAsync(string sessionId)
        {
            try
            {
                var (success, session, message) = await _persistenceService.ResumeSessionAsync(sessionId);
                if (!success || session == null)
                {
                    return $"Error: {message}";
                }

                var statusResponse = await _routerAgent.GetExecutionStatus(session);
                
                await _persistenceService.SaveSessionAsync(session);

                return $"{message}\n\n{statusResponse}";
            }
            catch (Exception ex)
            {
                return $"Error resuming campaign: {ex.Message}";
            }
        }

        /// <summary>
        /// Gets generated content for a campaign
        /// </summary>
        public async Task<string> GetGeneratedContentAsync(string sessionId, string? contentType = null)
        {
            try
            {
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {
                    return $"Session {sessionId} not found";
                }

                if (!session.Campaign.GeneratedContent.Any())
                {
                    return "No content has been generated yet. Please execute the campaign plan first.";
                }

                if (!string.IsNullOrEmpty(contentType))
                {
                    if (session.Campaign.GeneratedContent.TryGetValue(contentType, out var content))
                    {
                        return $"=== {contentType} ===\n\n{content}";
                    }
                    else
                    {
                        return $"Content type '{contentType}' not found. Available types: {string.Join(", ", session.Campaign.GeneratedContent.Keys)}";
                    }
                }

                var allContent = string.Join("\n\n" + new string('=', 50) + "\n\n", 
                    session.Campaign.GeneratedContent.Select(kv => $"=== {kv.Key} ===\n\n{kv.Value}"));

                return $"All Generated Content:\n\n{allContent}";
            }
            catch (Exception ex)
            {
                return $"Error getting generated content: {ex.Message}";
            }
        }

        /// <summary>
        /// Lists all active campaigns
        /// </summary>
        public async Task<string> ListActiveCampaignsAsync()
        {
            try
            {
                return await _persistenceService.GetSessionsSummaryAsync();
            }
            catch (Exception ex)
            {
                return $"Error listing campaigns: {ex.Message}";
            }
        }

        /// <summary>
        /// Approves a campaign for execution
        /// </summary>
        public async Task<string> ApproveCampaignAsync(string sessionId)
        {
            try
            {
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {
                    return $"Session {sessionId} not found";
                }

                if (session.Campaign.Status != CampaignStatus.ReadyForApproval)
                {
                    return $"Campaign is not ready for approval. Current status: {session.Campaign.Status}";
                }

                session.Campaign.Status = CampaignStatus.Approved;
                session.Campaign.ApprovedAt = DateTime.UtcNow;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Campaign approved for execution");

                await _persistenceService.SaveSessionAsync(session);

                return "✅ Campaign approved! It is now ready for execution.";
            }
            catch (Exception ex)
            {
                return $"Error approving campaign: {ex.Message}";
            }
        }

        /// <summary>
        /// Simulates executing an approved campaign
        /// </summary>
        public async Task<string> LaunchCampaignAsync(string sessionId)
        {
            try
            {
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {
                    return $"Session {sessionId} not found";
                }

                if (session.Campaign.Status != CampaignStatus.Approved)
                {
                    return $"Campaign must be approved before launch. Current status: {session.Campaign.Status}";
                }

                session.Campaign.Status = CampaignStatus.Executed;
                session.Campaign.ExecutedAt = DateTime.UtcNow;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Campaign launched successfully");

                await _persistenceService.SaveSessionAsync(session);

                return $@"
🎉 Campaign Launched Successfully!

Campaign: {session.Campaign.Goal}
Target Audience: {session.Campaign.Audience}
Components Deployed: {string.Join(", ", session.Campaign.GeneratedContent.Keys)}
Launch Time: {session.Campaign.ExecutedAt:yyyy-MM-dd HH:mm:ss}

Your campaign is now live and reaching your target audience!
";
            }
            catch (Exception ex)
            {
                return $"Error launching campaign: {ex.Message}";
            }
        }

        private Kernel CreateKernel(IConfiguration configuration)
        {
            var builder = Kernel.CreateBuilder();

            // Try to get Azure OpenAI configuration
            var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
            var azureOpenAIApiKey = configuration["AzureOpenAI:ApiKey"];
            var azureOpenAIDeployment = configuration["AzureOpenAI:DeploymentName"];

            if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIApiKey))
            {
                builder.AddAzureOpenAIChatCompletion(
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
                    builder.AddOpenAIChatCompletion(
                        modelId: "gpt-4",
                        apiKey: openAIApiKey);
                }
                else
                {
                    // For development/testing, create a basic kernel without AI service
                    Console.WriteLine("Warning: No AI service configured. Using basic kernel for testing.");
                }
            }

            return builder.Build();
        }
    }
}
