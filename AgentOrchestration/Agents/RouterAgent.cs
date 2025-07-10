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

                // Simulate human approval process
                if (step.RequiresApproval)
                {
                    var approvalResult = await SimulateApprovalProcess(step, session);
                    results.Add($"   📋 Approval: {approvalResult}");
                    
                    if (approvalResult.Contains("approved"))
                    {
                        session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Step {step.Name} approved");
                    }
                }

                // Add delay to simulate processing time
                await Task.Delay(1000);
            }

            // Mark campaign as ready for approval if all steps completed
            if (plan.CurrentStepIndex >= plan.Steps.Count)
            {
                session.Campaign.Status = CampaignStatus.ReadyForApproval;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Campaign execution completed");
                results.Add("\n🎉 Campaign execution completed! Ready for final approval.");
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
                
                default:
                    return $"Unknown researcher function: {step.Function}";
            }
        }

        private async Task<string> ExecuteContentGenerationStep(PlanStep step, CampaignSession session)
        {
            var brief = step.Parameters.GetValueOrDefault("brief", "").ToString() ?? "";
            var insights = session.Insights != null ? 
                $"Audience: {session.Insights.Audience}, Customers: {session.Insights.Customers.Count}" : "";

            switch (step.Function)
            {
                case "GenerateLandingPage":
                    var landingPage = await _contentTools.GenerateLandingPage(brief, insights);
                    session.Campaign.GeneratedContent["LandingPage"] = landingPage;
                    return "Landing page generated successfully";

                case "GenerateEmailDraft":
                    var emailDraft = await _contentTools.GenerateEmailDraft(brief, insights);
                    session.Campaign.GeneratedContent["EmailDraft"] = emailDraft;
                    return "Email draft generated successfully";

                case "GenerateLinkedInPost":
                    var linkedInPost = await _contentTools.GenerateLinkedInPost(brief, insights);
                    session.Campaign.GeneratedContent["LinkedInPost"] = linkedInPost;
                    return "LinkedIn post generated successfully";

                case "GenerateAdCopy":
                    var adCopy = await _contentTools.GenerateAdCopy(brief, insights);
                    session.Campaign.GeneratedContent["AdCopy"] = adCopy;
                    return "Ad copy generated successfully";

                case "GenerateContent":
                    var component = step.Parameters.GetValueOrDefault("component", "").ToString() ?? "";
                    var content = await _contentTools.GenerateContent(brief, component, insights);
                    session.Campaign.GeneratedContent[component] = content;
                    return $"{component} content generated successfully";

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

Generated Content:
{string.Join("\n", session.Campaign.GeneratedContent.Select(kv => $"- {kv.Key}: ✅ Ready"))}

All campaign components have been generated and are ready for deployment.
";

            return summary;
        }

        private async Task<string> SimulateApprovalProcess(PlanStep step, CampaignSession session)
        {
            // Simulate async approval process
            await Task.Delay(500);
            
            // For prototype, automatically approve all steps
            var approvalMessages = new[]
            {
                "Content reviewed and approved by marketing team",
                "Quality assurance passed - approved for use",
                "Brand guidelines compliance verified - approved",
                "Legal review completed - approved for publication"
            };

            var random = new Random();
            var message = approvalMessages[random.Next(approvalMessages.Length)];
            
            return $"✅ {message}";
        }

        public async Task<string> GetExecutionStatus(CampaignSession session)
        {
            if (session.Plan == null)
            {
                return "No campaign plan available.";
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

            return statusReport;
        }
    }
}
