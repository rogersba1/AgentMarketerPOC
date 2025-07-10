using Microsoft.Extensions.Configuration;
using AgentOrchestration.Services;
using System;
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
                Console.WriteLine("3. Create Campaign Plan");
                Console.WriteLine("4. Execute Campaign");
                Console.WriteLine("5. Get Campaign Status");
                Console.WriteLine("6. Resume Campaign");
                Console.WriteLine("7. View Generated Content");
                Console.WriteLine("8. List Active Campaigns");
                Console.WriteLine("9. Approve Campaign");
                Console.WriteLine("10. Launch Campaign");
                Console.WriteLine("11. Run Full Demo");
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
                    case "3":
                        await CreateCampaignPlan(orchestrationService, currentSessionId);
                        break;
                    case "4":
                        await ExecuteCampaign(orchestrationService, currentSessionId);
                        break;
                    case "5":
                        await GetCampaignStatus(orchestrationService, currentSessionId);
                        break;
                    case "6":
                        currentSessionId = await ResumeCampaign(orchestrationService);
                        break;
                    case "7":
                        await ViewGeneratedContent(orchestrationService, currentSessionId);
                        break;
                    case "8":
                        await ListActiveCampaigns(orchestrationService);
                        break;
                    case "9":
                        await ApproveCampaign(orchestrationService, currentSessionId);
                        break;
                    case "10":
                        await LaunchCampaign(orchestrationService, currentSessionId);
                        break;
                    case "11":
                        currentSessionId = await RunFullDemo(orchestrationService);
                        break;
                    case "0":
                        Console.WriteLine("Goodbye!");
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

        static async Task<string?> StartNewCampaign(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Start New Campaign ---");
            
            Console.Write("Enter campaign goal: ");
            var goal = Console.ReadLine() ?? "New AI-powered capabilities drive revenue growth";
            
            Console.Write("Enter target audience: ");
            var audience = Console.ReadLine() ?? "Top 20 customers";
            
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
            var (sessionId, response) = await orchestrationService.StartNewCampaignFromNaturalLanguageAsync(description);
            
            Console.WriteLine("\n" + response);
            
            return sessionId;
        }

        static async Task CreateCampaignPlan(CampaignOrchestrationService orchestrationService, string? sessionId)
        {
            Console.WriteLine("\n--- Create Campaign Plan ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            Console.Write("Enter additional planning instructions (or press Enter for default): ");
            var instructions = Console.ReadLine();

            var response = await orchestrationService.CreateCampaignPlanAsync(sessionId, instructions ?? "");
            Console.WriteLine("\n" + response);
        }

        static async Task ExecuteCampaign(CampaignOrchestrationService orchestrationService, string? sessionId)
        {
            Console.WriteLine("\n--- Execute Campaign ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            Console.WriteLine("Executing campaign plan...");
            var response = await orchestrationService.ExecuteCampaignAsync(sessionId);
            Console.WriteLine("\n" + response);
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

        static async Task ViewGeneratedContent(CampaignOrchestrationService orchestrationService, string? sessionId)
        {
            Console.WriteLine("\n--- View Generated Content ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            Console.Write("Enter content type (or press Enter for all): ");
            var contentType = Console.ReadLine();

            var response = await orchestrationService.GetGeneratedContentAsync(sessionId, contentType);
            Console.WriteLine("\n" + response);
        }

        static async Task ListActiveCampaigns(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Active Campaigns ---");
            
            var response = await orchestrationService.ListActiveCampaignsAsync();
            Console.WriteLine("\n" + response);
        }

        static async Task ApproveCampaign(CampaignOrchestrationService orchestrationService, string? sessionId)
        {
            Console.WriteLine("\n--- Approve Campaign ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            var response = await orchestrationService.ApproveCampaignAsync(sessionId);
            Console.WriteLine("\n" + response);
        }

        static async Task LaunchCampaign(CampaignOrchestrationService orchestrationService, string? sessionId)
        {
            Console.WriteLine("\n--- Launch Campaign ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            var response = await orchestrationService.LaunchCampaignAsync(sessionId);
            Console.WriteLine("\n" + response);
        }

        static async Task<string> RunFullDemo(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Full Demo: End-to-End Campaign Creation ---");
            Console.WriteLine("This demo will walk through the complete campaign creation process...\n");

            // Step 1: Start new campaign
            Console.WriteLine("🎯 Step 1: Starting new campaign...");
            var (sessionId, startResponse) = await orchestrationService.StartNewCampaignAsync(
                "AI-powered marketing capabilities drive 40% increase in campaign ROI",
                "Top 20 enterprise customers",
                new[] { "landing page", "email", "linkedin post", "ads" });
            
            Console.WriteLine(startResponse);
            await Task.Delay(2000);

            // Step 2: Create campaign plan
            Console.WriteLine("\n📋 Step 2: Creating campaign execution plan...");
            var planResponse = await orchestrationService.CreateCampaignPlanAsync(sessionId);
            Console.WriteLine(planResponse);
            await Task.Delay(2000);

            // Step 3: Execute campaign
            Console.WriteLine("\n⚡ Step 3: Executing campaign plan...");
            var executeResponse = await orchestrationService.ExecuteCampaignAsync(sessionId);
            Console.WriteLine(executeResponse);
            await Task.Delay(2000);

            // Step 4: Show status
            Console.WriteLine("\n📊 Step 4: Campaign execution status...");
            var statusResponse = await orchestrationService.GetCampaignStatusAsync(sessionId);
            Console.WriteLine(statusResponse);
            await Task.Delay(2000);

            // Step 5: Approve campaign
            Console.WriteLine("\n✅ Step 5: Approving campaign...");
            var approveResponse = await orchestrationService.ApproveCampaignAsync(sessionId);
            Console.WriteLine(approveResponse);
            await Task.Delay(2000);

            // Step 6: Launch campaign
            Console.WriteLine("\n🚀 Step 6: Launching campaign...");
            var launchResponse = await orchestrationService.LaunchCampaignAsync(sessionId);
            Console.WriteLine(launchResponse);

            Console.WriteLine("\n🎉 Demo completed! Campaign has been successfully created, executed, and launched.");
            Console.WriteLine($"Session ID: {sessionId}");

            return sessionId;
        }
    }
}
