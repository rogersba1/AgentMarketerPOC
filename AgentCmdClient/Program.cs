using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using AgentOrchestration.Services;
using AgentOrchestration.Services.Modern;
using AgentOrchestration.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AgentCmdClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Multi-Agent Campaign Orchestration System");
            Console.WriteLine(new string('=', 50));

            // Setup configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddUserSecrets<Program>()
                .Build();

            // Create kernel and services
            var kernelBuilder = Kernel.CreateBuilder();
            
            // Configure AI service
            var azureOpenAIKey = configuration["AzureOpenAI:ApiKey"];
            var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
            
            if (!string.IsNullOrEmpty(azureOpenAIKey) && !string.IsNullOrEmpty(azureOpenAIEndpoint))
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    "gpt-4o",
                    azureOpenAIEndpoint,
                    azureOpenAIKey);
            }
            else
            {
                // Fallback to OpenAI
                var openAIKey = configuration["OpenAI:ApiKey"];
                if (!string.IsNullOrEmpty(openAIKey))
                {
                    kernelBuilder.AddOpenAIChatCompletion("gpt-4o", openAIKey);
                }
                else
                {
                    Console.WriteLine("❌ No AI service configured. Please add OpenAI or Azure OpenAI configuration.");
                    return;
                }
            }
            
            var kernel = kernelBuilder.Build();
            
            // Create services
            var persistenceService = new ContextPersistenceService();
            var sequentialService = new SequentialCampaignOrchestrationService(kernel);

            // Display menu and handle user input
            await RunInteractiveDemo(sequentialService, persistenceService);
        }

        static async Task RunInteractiveDemo(SequentialCampaignOrchestrationService sequentialService, ContextPersistenceService persistenceService)
        {
            string? currentSessionId = null;
            
            while (true)
            {
                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("Sequential Campaign Orchestration");
                Console.WriteLine(new string('=', 50));
                Console.WriteLine("1. Start New Campaign");
                Console.WriteLine("2. Get Session Status");
                Console.WriteLine("3. List Sessions");
                Console.WriteLine("4. Resume Session");
                Console.WriteLine("5. Get Pending Approvals");
                Console.WriteLine("0. Exit");
                Console.WriteLine("\nCurrent Session: " + (currentSessionId ?? "None"));
                Console.Write("\nSelect option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        currentSessionId = await StartNewCampaign(sequentialService, persistenceService);
                        break;
                    case "2":
                        await GetSessionStatus(sequentialService, persistenceService, currentSessionId);
                        break;
                    case "3":
                        await ListSessions(persistenceService);
                        break;
                    case "4":
                        currentSessionId = await ResumeSession(sequentialService, persistenceService);
                        break;
                    case "5":
                        await GetPendingApprovals(sequentialService, currentSessionId);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }

                if (choice != "0")
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static async Task<string?> StartNewCampaign(SequentialCampaignOrchestrationService sequentialService, ContextPersistenceService persistenceService)
        {
            Console.WriteLine("\n--- Start New Campaign ---");
            
            Console.Write("Enter campaign description (e.g., 'Create a campaign for tech companies in retail'): ");
            var description = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(description))
            {
                description = "Create a multi-company marketing campaign for technology companies in the retail sector";
                Console.WriteLine($"Using default: {description}");
            }

            // Create new campaign session
            var sessionId = Guid.NewGuid().ToString();
            var campaign = new Campaign
            {
                Id = sessionId,
                Goal = description,
                Audience = "Technology companies in retail",
                Components = new List<string> { "landing page", "email", "ads" },
                CreatedAt = DateTime.UtcNow,
                ExecutionLog = new List<string>()
            };

            var session = new CampaignSession
            {
                Id = sessionId,
                Campaign = campaign,
                CurrentContext = description,
                LastUpdated = DateTime.UtcNow,
                IsActive = true
            };

            // Save session
            await persistenceService.SaveSessionAsync(session);
            
            // Execute campaign
            var result = await sequentialService.ExecuteCampaignSequentiallyAsync(session, description);
            
            Console.WriteLine("\n" + result);
            
            return sessionId;
        }

        static async Task GetSessionStatus(SequentialCampaignOrchestrationService sequentialService, ContextPersistenceService persistenceService, string? sessionId)
        {
            Console.WriteLine("\n--- Session Status ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            var session = await sequentialService.GetSessionAsync(sessionId);
            if (session == null)
            {
                Console.WriteLine($"Session {sessionId} not found.");
                return;
            }

            Console.WriteLine($"Session ID: {session.Id}");
            Console.WriteLine($"Campaign Goal: {session.Campaign.Goal}");
            Console.WriteLine($"Status: {session.Campaign.Status}");
            Console.WriteLine($"Created: {session.Campaign.CreatedAt}");
            Console.WriteLine($"Pending Approvals: {session.Campaign.PendingApprovals.Count}");
            
            if (session.Campaign.ExecutionLog.Any())
            {
                Console.WriteLine("\nExecution Log (last 5 entries):");
                foreach (var entry in session.Campaign.ExecutionLog.TakeLast(5))
                {
                    Console.WriteLine($"  {entry}");
                }
            }
        }

        static async Task ListSessions(ContextPersistenceService persistenceService)
        {
            Console.WriteLine("\n--- Active Sessions ---");
            
            var sessions = await persistenceService.GetActiveSessionsAsync();
            
            if (!sessions.Any())
            {
                Console.WriteLine("No active sessions found.");
                return;
            }

            foreach (var session in sessions)
            {
                Console.WriteLine($"📋 {session.Id}: {session.Campaign.Goal} ({session.Campaign.Status})");
            }
        }

        static async Task<string?> ResumeSession(SequentialCampaignOrchestrationService sequentialService, ContextPersistenceService persistenceService)
        {
            Console.WriteLine("\n--- Resume Session ---");
            
            Console.Write("Enter session ID to resume: ");
            var sessionId = Console.ReadLine();
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("Session ID is required.");
                return null;
            }

            var result = await sequentialService.ContinueCampaignExecutionAsync(sessionId);
            Console.WriteLine("\n" + result);
            
            return sessionId;
        }

        static async Task GetPendingApprovals(SequentialCampaignOrchestrationService sequentialService, string? sessionId)
        {
            Console.WriteLine("\n--- Pending Approvals ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            var approvals = await sequentialService.GetPendingCompanyBriefs(sessionId);
            
            if (!approvals.Any())
            {
                Console.WriteLine("No pending approvals found.");
                return;
            }

            foreach (var approval in approvals)
            {
                Console.WriteLine($"🔍 Pending approval: {approval}");
            }
        }
    }
}
