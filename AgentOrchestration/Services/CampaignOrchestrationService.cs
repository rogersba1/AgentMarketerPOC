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
            _plannerAgent = new PlannerAgent(_kernel, _companyDataService);
            _routerAgent = new RouterAgent(_kernel, _researcherAgent, _contentTools);
            _persistenceService = new ContextPersistenceService();
            _parsingService = new CampaignParsingService(_kernel);

            // Initialize company data
            _ = _companyDataService.LoadCompanyDataAsync();

            // Register content generation tools with the kernel
            _kernel.Plugins.AddFromObject(_contentTools);
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
        /// Gets a campaign session by ID
        /// </summary>
        public async Task<CampaignSession> GetSessionAsync(string sessionId)
        {
            var session = await _persistenceService.LoadSessionAsync(sessionId);
            if (session == null)
            {
                throw new ArgumentException($"Session not found: {sessionId}");
            }
            return session;
        }

        /// <summary>
        /// Starts a new campaign from natural language input and automatically proceeds through planning and execution,
        /// stopping only for human approvals (like company brief reviews)
        /// </summary>
        public async Task<(string sessionId, string response, bool hasApprovals)> StartAndExecuteCampaignAsync(string naturalLanguageInput)
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

                var initialResponse = $@"🚀 **Campaign Created Successfully!**

**Campaign Details:**
- Goal: {parsedCampaign.Goal}
- Target Audience: {parsedCampaign.Audience}  
- Components: {string.Join(", ", parsedCampaign.Components)}

🔄 **Generating execution plan...**";

                // Step 2: Create the execution plan
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Creating execution plan");
                var planResponse = await _plannerAgent.ProcessAsync("Create a comprehensive campaign execution plan", session);
                
                if (session.Plan == null)
                {
                    return (session.Id, initialResponse + "\n\n❌ **Error:** Failed to create execution plan.", false);
                }

                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Plan created with {session.Plan.Steps.Count} steps");
                
                var planSummary = $"\n\n✅ **Execution Plan Created** ({session.Plan.Steps.Count} steps)";
                
                // Step 3: Start execution, which will stop at company briefs requiring approval
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] System: Starting campaign execution");
                session.Campaign.Status = CampaignStatus.InProgress;
                
                var executionResponse = await _routerAgent.ExecutePlan(session);
                
                await _persistenceService.SaveSessionAsync(session);

                // Check if execution stopped for approvals
                var hasApprovals = session.Plan.Steps.Any(s => s.RequiresHumanApproval && s.ApprovalStatus == HumanApprovalStatus.PendingReview);
                
                if (hasApprovals)
                {
                    var finalResponse = initialResponse + planSummary + 
                        "\n\n🔄 **Campaign execution started...**" +
                        "\n\n" + executionResponse;
                    
                    return (session.Id, finalResponse, true);
                }
                else
                {
                    // Campaign completed without needing approvals
                    session.Campaign.Status = CampaignStatus.Executed;
                    await _persistenceService.SaveSessionAsync(session);
                    
                    var finalResponse = initialResponse + planSummary + 
                        "\n\n✅ **Campaign completed successfully!**" +
                        "\n\n" + executionResponse;
                    
                    return (session.Id, finalResponse, false);
                }
            }
            catch (Exception ex)
            {
                return (string.Empty, $"❌ **Error starting and executing campaign:** {ex.Message}", false);
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
