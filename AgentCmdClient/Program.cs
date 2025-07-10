using Microsoft.Extensions.Configuration;
using AgentOrchestration.Services;
using System;
using System.Threading;
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
                Console.WriteLine("9. Review Company Briefs"); // New option
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
                        await ReviewCompanyBriefs(orchestrationService, currentSessionId);
                        break;
                    case "10":
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

            Console.WriteLine("🚀 Executing campaign plan...");
            Console.WriteLine("⏳ This may take a moment for multi-company campaigns...");
            
            // Show a simple progress indicator
            var progressTask = ShowProgressIndicator("Generating content");
            
            try
            {
                var response = await orchestrationService.ExecuteCampaignAsync(sessionId);
                progressTask.Cancel();
                Console.WriteLine("\r✅ Execution completed!                    ");
                Console.WriteLine("\n" + response);
            }
            catch (Exception ex)
            {
                progressTask.Cancel();
                Console.WriteLine("\r❌ Execution failed!                      ");
                Console.WriteLine($"\nError: {ex.Message}");
            }
        }

        static CancellationTokenSource ShowProgressIndicator(string message)
        {
            var cts = new CancellationTokenSource();
            
            Task.Run(async () =>
            {
                var spinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
                int i = 0;
                
                while (!cts.Token.IsCancellationRequested)
                {
                    Console.Write($"\r{spinner[i % spinner.Length]} {message}...");
                    i++;
                    await Task.Delay(100, cts.Token);
                }
            }, cts.Token);
            
            return cts;
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

        static async Task<string> RunFullDemo(CampaignOrchestrationService orchestrationService)
        {
            Console.WriteLine("\n--- Full Demo: End-to-End Campaign Creation ---");
            Console.WriteLine("This demo will walk through the complete campaign creation process...\n");

            // Step 1: Start new campaign
            Console.WriteLine("🎯 Step 1: Starting new campaign...");
            var (sessionId, startResponse) = await orchestrationService.StartNewCampaignAsync(
                "AI-powered marketing capabilities drive 40% increase in campaign ROI",
                "Top 20 retail customers",
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

            // Step 4: Show final status
            Console.WriteLine("\n📊 Step 4: Final campaign status...");
            var statusResponse = await orchestrationService.GetCampaignStatusAsync(sessionId);
            Console.WriteLine(statusResponse);

            Console.WriteLine("\n🎉 Demo completed! Campaign has been successfully created and executed.");
            Console.WriteLine($"Session ID: {sessionId}");

            return sessionId;
        }

        static async Task ReviewCompanyBriefs(CampaignOrchestrationService orchestrationService, string? sessionId)
        {
            Console.WriteLine("\n--- Review Company Briefs ---");
            
            if (string.IsNullOrEmpty(sessionId))
            {
                Console.WriteLine("No active session. Please start a new campaign first.");
                return;
            }

            while (true)
            {
                Console.WriteLine("\nCompany Brief Review Commands:");
                Console.WriteLine("• list - Show pending approvals");
                Console.WriteLine("• review [company name] - Review a company brief");
                Console.WriteLine("• approve [company name] - Approve a company brief");
                Console.WriteLine("• modify [company name] [feedback] - Approve with modifications");
                Console.WriteLine("• reject [company name] [reason] - Reject a company brief");
                Console.WriteLine("• continue - Continue campaign execution");
                Console.WriteLine("• back - Return to main menu");
                
                Console.Write("\nEnter command: ");
                var input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                    continue;

                var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

                try
                {
                    switch (command)
                    {
                        case "list":
                            var pendingList = await orchestrationService.GetRouterAgent().ListPendingApprovals(
                                await orchestrationService.GetSessionAsync(sessionId));
                            Console.WriteLine(pendingList);
                            break;

                        case "review":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("❌ Usage: review [company name]");
                                break;
                            }
                            var companyToReview = string.Join(" ", parts.Skip(1));
                            var reviewResult = await orchestrationService.GetRouterAgent().ReviewCompanyBriefForApproval(
                                companyToReview, await orchestrationService.GetSessionAsync(sessionId));
                            Console.WriteLine(reviewResult);
                            break;

                        case "approve":
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("❌ Usage: approve [company name]");
                                break;
                            }
                            var companyToApprove = string.Join(" ", parts.Skip(1));
                            var approveResult = await orchestrationService.GetRouterAgent().ApproveBrief(
                                companyToApprove, "Approved", await orchestrationService.GetSessionAsync(sessionId), 
                                isApproved: true, isModified: false);
                            Console.WriteLine(approveResult);
                            break;

                        case "modify":
                            if (parts.Length < 3)
                            {
                                Console.WriteLine("❌ Usage: modify [company name] [your feedback]");
                                break;
                            }
                            var companyToModify = parts[1];
                            var feedback = string.Join(" ", parts.Skip(2));
                            var modifyResult = await orchestrationService.GetRouterAgent().ApproveBrief(
                                companyToModify, feedback, await orchestrationService.GetSessionAsync(sessionId), 
                                isApproved: true, isModified: true);
                            Console.WriteLine(modifyResult);
                            break;

                        case "reject":
                            if (parts.Length < 3)
                            {
                                Console.WriteLine("❌ Usage: reject [company name] [reason]");
                                break;
                            }
                            var companyToReject = parts[1];
                            var reason = string.Join(" ", parts.Skip(2));
                            var rejectResult = await orchestrationService.GetRouterAgent().ApproveBrief(
                                companyToReject, reason, await orchestrationService.GetSessionAsync(sessionId), 
                                isApproved: false, isModified: false);
                            Console.WriteLine(rejectResult);
                            break;

                        case "continue":
                            Console.WriteLine("🚀 Continuing campaign execution...");
                            var continueResult = await orchestrationService.ExecuteCampaignAsync(sessionId);
                            Console.WriteLine(continueResult);
                            break;

                        case "back":
                            return;

                        default:
                            Console.WriteLine("❌ Unknown command. Type 'back' to return to main menu.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error: {ex.Message}");
                }
            }
        }
    }
}
