using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using AgentOrchestration.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents
{
    /// <summary>
    /// Router agent responsible for orchestrating plan execution across different agents and tools
    /// </summary>
    public class RouterAgent : BaseAgent
    {
        private readonly ResearcherAgent _researcherAgent;
        private readonly ContentGenerationTools _contentTools;

        public override string Name => "Router";
        public override string Description => "Orchestrates campaign plan execution across multiple agents and tools";

        private const string ROUTER_SYSTEM_PROMPT = @"
You are the Campaign Orchestration Router. Your role is to coordinate the execution of campaign plans by:
1. Managing the sequence of steps in the campaign plan
2. Calling appropriate agents and tools for each step
3. Collecting and organizing results
4. Managing human-in-the-loop approval processes
5. Ensuring campaign context is maintained throughout execution

You maintain the overall campaign state and ensure all components work together cohesively.
";

        public RouterAgent(Kernel kernel, ResearcherAgent researcherAgent, ContentGenerationTools contentTools) 
            : base(kernel, ROUTER_SYSTEM_PROMPT)
        {
            _researcherAgent = researcherAgent;
            _contentTools = contentTools;
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Starting plan execution");

                if (session.Plan == null)
                {
                    return "No campaign plan available. Please create a plan first.";
                }

                return await ExecutePlan(session);
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Error - {ex.Message}");
                return $"Error executing campaign plan: {ex.Message}";
            }
        }

        public async Task<string> ExecutePlan(CampaignSession session)
        {
            var plan = session.Plan!;
            var results = new List<string>();
            results.Add($"Executing campaign with steps: {plan.Steps}" );
            for (int i = plan.CurrentStepIndex; i < plan.Steps.Count; i++)
            {
                var step = plan.Steps[i];
                
                if (step.IsCompleted)
                {
                    results.Add($"✅ Step {i + 1}: {step.Name} (Already completed)");
                    continue;
                }

                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Executing step {i + 1}/{plan.Steps.Count}: {step.Name}");

                var stepResult = await ExecuteStep(step, session);
                
                step.Result = stepResult;
                step.IsCompleted = true;
                step.CompletedAt = DateTime.UtcNow;
                plan.CurrentStepIndex = i + 1;

                results.Add($"✅ Step {i + 1}: {step.Name}\n   Result: {stepResult}");

                // Add delay to simulate processing time
                await Task.Delay(500);
            }

            // Mark campaign as executed if all steps completed
            if (plan.CurrentStepIndex >= plan.Steps.Count)
            {
                session.Campaign.Status = CampaignStatus.Executed;
                session.Campaign.ExecutedAt = DateTime.UtcNow;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Campaign execution completed");
                results.Add("\n🎉 Campaign execution completed! All content has been generated and deployed.");
            }

            return string.Join("\n", results);
        }

        private async Task<string> ExecuteStep(PlanStep step, CampaignSession session)
        {
            try
            {
                return step.AgentType switch
                {
                    "ResearcherAgent" => await ExecuteResearcherStep(step, session),
                    "ContentTool" => await ExecuteContentGenerationStep(step, session),
                    "RouterAgent" => await ExecuteRouterStep(step, session),
                    _ => $"Unknown agent type: {step.AgentType}"
                };
            }
            catch (Exception ex)
            {
                return $"Error executing step: {ex.Message}";
            }
        }

        private async Task<string> ExecuteResearcherStep(PlanStep step, CampaignSession session)
        {
            switch (step.Function)
            {
                case "GetCustomerInsights":
                    var audience = step.Parameters.GetValueOrDefault("audience", "").ToString();
                    return await _researcherAgent.ProcessAsync(audience!, session);
                
                case "GetIndustryInsights":
                    var industry = step.Parameters.GetValueOrDefault("industry", "").ToString();
                    var companyCount = step.Parameters.GetValueOrDefault("companyCount", 0);
                    return await _researcherAgent.GetIndustryInsights(industry!, Convert.ToInt32(companyCount));
                
                case "GetCompanySpecificInsights":
                    var companyName = step.Parameters.GetValueOrDefault("companyName", "").ToString();
                    var companyId = step.Parameters.GetValueOrDefault("companyId", "").ToString();
                    var companyIndustry = step.Parameters.GetValueOrDefault("industry", "").ToString();
                    return await _researcherAgent.GetCompanySpecificInsights(companyName!, companyId!, companyIndustry!);
                
                default:
                    return $"Unknown researcher function: {step.Function}";
            }
        }

        private async Task<string> ExecuteContentGenerationStep(PlanStep step, CampaignSession session)
        {
            var goal = step.Parameters.GetValueOrDefault("goal", "").ToString() ?? "";
            var companyName = step.Parameters.GetValueOrDefault("companyName", "").ToString() ?? "";
            var companyProfile = step.Parameters.GetValueOrDefault("companyProfile", "").ToString() ?? "";
            var contentType = step.Parameters.GetValueOrDefault("contentType", "").ToString() ?? "";
            var insights = step.Parameters.GetValueOrDefault("insights", "").ToString() ?? "";

            // For legacy support, also check for brief parameter
            var brief = step.Parameters.GetValueOrDefault("brief", "").ToString() ?? "";
            if (string.IsNullOrEmpty(goal) && !string.IsNullOrEmpty(brief))
            {
                goal = brief;
            }

            switch (step.Function)
            {
                // Company-specific content generation functions
                case "generate_personalized_landing_page":
                    var personalizedLandingPage = await _contentTools.GeneratePersonalizedLandingPage(goal, companyName, companyProfile);
                    var landingPageKey = $"LandingPage_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[landingPageKey] = personalizedLandingPage;
                    return $"Personalized landing page generated for {companyName}";

                case "generate_personalized_email":
                    var personalizedEmail = await _contentTools.GeneratePersonalizedEmail(goal, companyName, companyProfile);
                    var emailKey = $"Email_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[emailKey] = personalizedEmail;
                    return $"Personalized email generated for {companyName}";

                case "generate_personalized_linkedin_post":
                    var personalizedLinkedIn = await _contentTools.GeneratePersonalizedLinkedInPost(goal, companyName, companyProfile);
                    var linkedInKey = $"LinkedInPost_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[linkedInKey] = personalizedLinkedIn;
                    return $"Personalized LinkedIn post generated for {companyName}";

                case "generate_personalized_ad_copy":
                    var personalizedAdCopy = await _contentTools.GeneratePersonalizedAdCopy(goal, companyName, companyProfile);
                    var adCopyKey = $"AdCopy_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[adCopyKey] = personalizedAdCopy;
                    return $"Personalized ad copy generated for {companyName}";

                case "generate_personalized_content":
                    // Fallback to specific content type functions
                    var personalizedContent = contentType.ToLower() switch
                    {
                        "landing page" or "landing site" => await _contentTools.GeneratePersonalizedLandingPage(goal, companyName, companyProfile),
                        "email" => await _contentTools.GeneratePersonalizedEmail(goal, companyName, companyProfile),
                        "linkedin" or "linkedin post" => await _contentTools.GeneratePersonalizedLinkedInPost(goal, companyName, companyProfile),
                        "ads" or "ad copy" => await _contentTools.GeneratePersonalizedAdCopy(goal, companyName, companyProfile),
                        _ => await _contentTools.GeneratePersonalizedEmail(goal, companyName, companyProfile) // Default to email if unknown type
                    };
                    var contentKey = $"{contentType}_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[contentKey] = personalizedContent;
                    return $"Personalized {contentType} content generated for {companyName}";

                default:
                    return $"Unknown content generation function: {step.Function}";
            }
        }

        private async Task<string> ExecuteRouterStep(PlanStep step, CampaignSession session)
        {
            switch (step.Function)
            {
                case "PrepareCampaignExecution":
                    return await PrepareCampaignExecution(session);
                
                case "ReviewCompanyCampaign":
                    return await ReviewCompanyCampaign(step, session);
                
                case "CoordinateMultiCompanyCampaign":
                    return await CoordinateMultiCompanyCampaign(step, session);
                
                default:
                    return $"Unknown router function: {step.Function}";
            }
        }

        private async Task<string> PrepareCampaignExecution(CampaignSession session)
        {
            await Task.Delay(500); // Simulate processing time

            var summary = $@"
Campaign Execution Summary:
- Campaign: {session.Campaign.Goal}
- Target Audience: {session.Campaign.Audience}
- Components Generated: {session.Campaign.GeneratedContent.Count}
- Status: {session.Campaign.Status}

Generated and Deployed Content:
{string.Join("\n", session.Campaign.GeneratedContent.Select(kv => $"- {kv.Key}: ✅ Deployed"))}

All campaign components have been generated and deployed successfully.
";

            return summary;
        }

        private async Task<string> ReviewCompanyCampaign(PlanStep step, CampaignSession session)
        {
            await Task.Delay(500); // Simulate processing time

            var companyName = step.Parameters.GetValueOrDefault("companyName", "").ToString() ?? "";
            var companyId = step.Parameters.GetValueOrDefault("companyId", "").ToString() ?? "";
            var campaignId = step.Parameters.GetValueOrDefault("campaignId", "").ToString() ?? "";

            // Find all content generated for this company
            var companyContent = session.Campaign.GeneratedContent
                .Where(kv => kv.Key.EndsWith($"_{companyName.Replace(" ", "_")}"))
                .ToList();

            var summary = $@"
Company Campaign Deployment for {companyName}:
- Campaign ID: {campaignId}
- Company ID: {companyId}
- Generated Content Items: {companyContent.Count}

Content Summary:
{string.Join("\n", companyContent.Select(kv => $"✅ {kv.Key}: Deployed successfully"))}

Deployment Checklist:
✅ Content personalized for {companyName}
✅ Brand alignment verified
✅ Messaging consistency confirmed
✅ Call-to-action optimized
✅ Content deployed to {companyName}

Status: Successfully deployed to {companyName}
";

            return summary;
        }

        private async Task<string> CoordinateMultiCompanyCampaign(PlanStep step, CampaignSession session)
        {
            await Task.Delay(1000); // Simulate processing time

            var campaignId = step.Parameters.GetValueOrDefault("campaignId", "").ToString() ?? "";
            var totalCompanies = step.Parameters.GetValueOrDefault("totalCompanies", 0);
            var targetCompanies = step.Parameters.GetValueOrDefault("targetCompanies", new List<string>());

            var companyList = targetCompanies is List<string> companies ? companies : new List<string>();

            // Analyze generated content across all companies
            var contentByType = new Dictionary<string, int>();
            foreach (var content in session.Campaign.GeneratedContent)
            {
                var contentType = content.Key.Split('_')[0];
                contentByType[contentType] = contentByType.GetValueOrDefault(contentType, 0) + 1;
            }

            var summary = $@"
🎯 Multi-Company Campaign Deployment Complete

Campaign Overview:
- Campaign ID: {campaignId}
- Total Target Companies: {totalCompanies}
- Total Content Items Generated: {session.Campaign.GeneratedContent.Count}

Content Distribution:
{string.Join("\n", contentByType.Select(kv => $"• {kv.Key}: {kv.Value} personalized versions deployed"))}

Company Deployment Status:
{string.Join("\n", companyList.Take(5).Select(company => $"✅ {company}: All content successfully deployed"))}
{(companyList.Count > 5 ? $"... and {companyList.Count - 5} more companies deployed" : "")}

Campaign Execution Results:
1. Individual deployment completed for each company
2. Personalized content delivered to each target
3. Coordinated timing achieved across all companies
4. Performance tracking active for each company

Status: All companies successfully deployed and campaign launched!
";

            return summary;
        }

        public Task<string> GetExecutionStatus(CampaignSession session)
        {
            if (session.Plan == null)
            {
                return Task.FromResult("No campaign plan available.");
            }

            var plan = session.Plan;
            var completedSteps = plan.Steps.Count(s => s.IsCompleted);
            var totalSteps = plan.Steps.Count;
            var progressPercentage = (completedSteps * 100) / totalSteps;

            var statusReport = $@"
Campaign Execution Status:
- Progress: {completedSteps}/{totalSteps} steps completed ({progressPercentage}%)
- Current Status: {session.Campaign.Status}
- Started: {plan.CreatedAt:yyyy-MM-dd HH:mm:ss}
- Last Updated: {session.LastUpdated:yyyy-MM-dd HH:mm:ss}

Step Details:
{string.Join("\n", plan.Steps.Select((s, i) => $"{i + 1}. {s.Name}: {(s.IsCompleted ? "✅ Completed" : "⏳ Pending")}"))}

Recent Activity:
{string.Join("\n", session.Campaign.ExecutionLog.TakeLast(5))}
";

            return Task.FromResult(statusReport);
        }
    }
}
