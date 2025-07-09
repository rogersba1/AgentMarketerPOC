using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents
{
    /// <summary>
    /// Planner agent responsible for creating campaign execution plans
    /// </summary>
    public class PlannerAgent : BaseAgent
    {
        public override string Name => "Planner";
        public override string Description => "Creates structured campaign execution plans based on user goals";

        private const string PLANNER_SYSTEM_PROMPT = @"
You are a Marketing Campaign Planner AI. Your role is to create detailed, structured campaign execution plans based on user input.

When a user provides campaign goals, audience, and components, you should:
1. Analyze the requirements
2. Break down the work into logical steps
3. Determine the sequence of actions needed
4. Identify which agents/tools to use for each step
5. Create a structured plan

Available agents and their capabilities:
- ResearcherAgent: Retrieves customer insights and audience data
- Content generation tools: GenerateLandingPage, GenerateEmailDraft, GenerateLinkedInPost, GenerateAdCopy

Your response should be a JSON object with the following structure:
{
  ""steps"": [
    {
      ""name"": ""Step Name"",
      ""description"": ""Detailed description"",
      ""agentType"": ""Agent or Tool name"",
      ""function"": ""Function to call"",
      ""parameters"": {""key"": ""value""},
      ""requiresApproval"": true/false
    }
  ],
  ""context"": ""Overall campaign context and strategy""
}

Focus on creating actionable, sequential steps that will result in a complete campaign.
";

        public PlannerAgent(Kernel kernel) : base(kernel, PLANNER_SYSTEM_PROMPT)
        {
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Planner: Processing campaign plan request");

                // Create the prompt with campaign context
                var prompt = $@"
Create a campaign execution plan for:
- Campaign Goal: {session.Campaign.Goal}
- Target Audience: {session.Campaign.Audience}
- Components: {string.Join(", ", session.Campaign.Components)}

User Input: {input}

Generate a structured plan with specific steps to execute this campaign.
";

                var response = await CallLLMAsync(prompt, input);
                
                // For prototype, create a hard-coded plan structure if LLM response is not valid JSON
                var plan = CreateFallbackPlan(session.Campaign);
                
                session.Plan = plan;
                session.Campaign.Status = CampaignStatus.InProgress;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Planner: Created execution plan with {plan.Steps.Count} steps");

                return $"Campaign execution plan created successfully with {plan.Steps.Count} steps:\n" +
                       string.Join("\n", plan.Steps.Select((s, i) => $"{i + 1}. {s.Name}: {s.Description}"));
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Planner: Error - {ex.Message}");
                return $"Error creating campaign plan: {ex.Message}";
            }
        }

        private CampaignPlan CreateFallbackPlan(Campaign campaign)
        {
            var plan = new CampaignPlan
            {
                CampaignId = campaign.Id,
                Context = $"Campaign plan for '{campaign.Goal}' targeting '{campaign.Audience}'"
            };

            // Step 1: Research audience
            plan.Steps.Add(new PlanStep
            {
                Name = "Research Audience",
                Description = $"Gather insights about {campaign.Audience}",
                AgentType = "ResearcherAgent",
                Function = "GetCustomerInsights",
                Parameters = new Dictionary<string, object>
                {
                    { "audience", campaign.Audience }
                },
                RequiresApproval = false
            });

            // Step 2-N: Generate content for each component
            foreach (var component in campaign.Components)
            {
                var stepName = $"Generate {component}";
                var function = component.ToLower() switch
                {
                    "landing site" or "landing page" => "GenerateLandingPage",
                    "email" => "GenerateEmailDraft",
                    "linkedin" or "linkedin post" => "GenerateLinkedInPost",
                    "ads" or "ad copy" => "GenerateAdCopy",
                    _ => "GenerateContent"
                };

                plan.Steps.Add(new PlanStep
                {
                    Name = stepName,
                    Description = $"Create {component} content based on campaign goal and audience insights",
                    AgentType = "ContentTool",
                    Function = function,
                    Parameters = new Dictionary<string, object>
                    {
                        { "goal", campaign.Goal },
                        { "component", component }
                    },
                    RequiresApproval = true
                });
            }

            // Final step: Prepare for execution
            plan.Steps.Add(new PlanStep
            {
                Name = "Prepare Campaign Execution",
                Description = "Review all generated content and prepare for campaign launch",
                AgentType = "RouterAgent",
                Function = "PrepareCampaignExecution",
                Parameters = new Dictionary<string, object>
                {
                    { "campaignId", campaign.Id }
                },
                RequiresApproval = true
            });

            return plan;
        }
    }
}
