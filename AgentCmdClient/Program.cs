using Microsoft.Extensions.Configuration;
using AgentOrchestration.Services;
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

            // Create orchestration service
            var orchestrationService = new CampaignOrchestrationService(configuration);

            // Display menu and handle user input
            await RunInteractiveDemo(orchestrationService);
        }

        static async Task RunInteractiveDemo(CampaignOrchestrationService orchestrationService)
        {
            string? currentSessionId = null;
            
            while (true)
            {
                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("Campaign Orchestration Menu");
                Console.WriteLine(new string('=', 50));
                Console.WriteLine("1. Start New Campaign");
                Console.WriteLine("2. Create Campaign with Natural Language");
                Console.WriteLine("5. Get Campaign Status");
                Console.WriteLine("6. Resume Campaign");
                
                Console.WriteLine("8. List Active Campaigns");
                
                Console.WriteLine("10. Run Full Demo");
                Console.WriteLine("0. Exit");
                Console.WriteLine("\nCurrent Session: " + (currentSessionId ?? "None"));
                Console.Write("\nSelect option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        currentSessionId = await StartNewCampaign(orchestrationService);
                        break;
                    case "2":
                        currentSessionId = await CreateCampaignNaturalLanguage(orchestrationService);
                        break;
                    case "5":
                        await GetCampaignStatus(orchestrationService, currentSessionId);
                        break;
                    case "6":
                        currentSessionId = await ResumeCampaign(orchestrationService);
                        break;
                    case "8":
                        await ListActiveCampaigns(orchestrationService);
                        break;

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

        static async Task<string?> StartNewCampaign(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Start New Campaign ---");
            
            Console.Write("Enter campaign goal: ");
            var goal = Console.ReadLine() ?? "New AI-powered capabilities drive revenue growth";
            
            Console.Write("Enter target audience: ");
            var audience = Console.ReadLine() ?? "Top 20 retail customers";
            
            Console.Write("Enter components (comma-separated): ");
            var componentsInput = Console.ReadLine() ?? "landing site, images, email, ads";
            var components = componentsInput.Split(',').Select(c => c.Trim()).ToArray();

            var (sessionId, response) = await orchestrationService.StartNewCampaignAsync(goal, audience, components);
            
            Console.WriteLine("\n" + response);
            
            return sessionId;
        }

        static async Task<string?> CreateCampaignNaturalLanguage(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Create Campaign with Natural Language ---");
            Console.WriteLine("Describe your campaign in natural language. Include:");
            Console.WriteLine("- Your marketing goal/objective");
            Console.WriteLine("- Target audience");
            Console.WriteLine("- Components you want (landing page, email, ads, etc.)");
            Console.WriteLine("\nExample: \"I'd like to create a campaign to highlight manufacturing automation for our top 20 customers that builds a landing page and generates emails\"");
            Console.WriteLine();
            
            Console.Write("Campaign description: ");
            var description = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(description))
            {
                Console.WriteLine("No description provided. Please try again.");
                return null;
            }

            // Use the orchestration service to create campaign from natural language
            var (sessionId, response, hasApprovals) = await orchestrationService.StartAndExecuteCampaignAsync(description);
            
            Console.WriteLine("\n" + response);
            
            return sessionId;
        }


        static async Task GetCampaignStatus(CampaignOrchestrationService orchestrationService, string? sessionId)
        {
            Console.WriteLine("\n--- Campaign Status ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            var response = await orchestrationService.GetCampaignStatusAsync(sessionId);
            Console.WriteLine("\n" + response);
        }

        static async Task<string?> ResumeCampaign(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Resume Campaign ---");
            
            Console.Write("Enter session ID to resume: ");
            var sessionId = Console.ReadLine();
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("Session ID is required.");
                return null;
            }

            var response = await orchestrationService.ResumeCampaignAsync(sessionId);
            Console.WriteLine("\n" + response);
            
            return sessionId;
        }

        static async Task ListActiveCampaigns(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Active Campaigns ---");
            
            var response = await orchestrationService.ListActiveCampaignsAsync();
            Console.WriteLine("\n" + response);
        }
    }
}
