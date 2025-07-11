using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using AgentOrchestration.Models;
using AgentOrchestration.Agents.Modern;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents.Modern
{
    /// <summary>
    /// Modern Planner agent using ChatCompletionAgent architecture
    /// </summary>
    public class ModernPlannerAgent : ModernAgentBase
    {
        public override string Name => "Modern Planner";
        public override string Description => "Creates comprehensive campaign execution plans using modern SK orchestration";

        private const string PLANNER_INSTRUCTIONS = @"
You are an expert Campaign Planning Agent specializing in multi-company marketing campaigns. Your role is to:

1. **Analyze Requirements**: Parse natural language campaign requests to extract goals, target audience, and content requirements
2. **Create Strategic Plans**: Design comprehensive, step-by-step execution plans with proper agent coordination
3. **Company Research**: Identify target companies and plan individual execution strategies
4. **Human-in-the-Loop**: Plan approval points where human oversight is needed
5. **Orchestration Ready**: Create plans optimized for modern agent orchestration patterns

Key Principles:
- Break complex campaigns into manageable, sequential steps
- Ensure each step has clear success criteria and dependencies
- Plan for human approval at critical decision points
- Optimize for parallel execution where possible
- Include detailed context for downstream agents

Your output should be detailed execution plans that can be directly used by orchestration systems.
";

        public ModernPlannerAgent(Kernel kernel) 
            : base(ModernAgentFactory.CreateAgent(
                "Modern Planner", 
                PLANNER_INSTRUCTIONS, 
                kernel))
        {
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Modern Planner: Processing campaign planning request");

                // Enhanced context for modern agent
                var enhancedInput = $@"
Campaign Planning Request: {input}

Current Campaign Context:
- Campaign ID: {session.Campaign.Id}
- Goal: {session.Campaign.Goal}
- Target Audience: {session.Campaign.Audience}
- Required Components: {string.Join(", ", session.Campaign.Components)}

Please create a comprehensive execution plan with:
1. Sequential steps for campaign execution
2. Company research and brief generation phases
3. Content generation coordination
4. Human approval points
5. Final deployment orchestration

The plan should be optimized for modern agent orchestration patterns and include specific agent assignments for each step.
";

                var planningResult = await InvokeAgentAsync(enhancedInput, session);

                // Create the execution plan
                var plan = CreateExecutionPlan(session, planningResult);
                session.Plan = plan;

                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Modern Planner: Created execution plan with {plan.Steps.Count} steps");

                return $@"
🎯 **Modern Campaign Plan Created Successfully!**

**Enhanced Planning Approach:**
- Leveraging modern agent orchestration patterns
- Optimized for {session.Campaign.Components.Count} content types across multiple companies
- Built-in human-in-the-loop approval workflows
- Parallel execution opportunities identified

**Plan Overview:**
- Total Steps: {plan.Steps.Count}
- Approval Points: {plan.Steps.Count(s => s.RequiresHumanApproval)}
- Estimated Execution Time: {EstimateExecutionTime(plan)} minutes

**Key Features:**
✅ Modern agent coordination
✅ Optimized orchestration patterns
✅ Enhanced human oversight
✅ Scalable execution framework

{planningResult}

**Next Steps:**
The plan is ready for execution using modern SequentialOrchestration and GroupChatOrchestration patterns.
Use 'execute plan' to begin orchestrated campaign execution.
";
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Modern Planner: Error - {ex.Message}");
                return $"Error creating campaign plan: {ex.Message}";
            }
        }

        private CampaignPlan CreateExecutionPlan(CampaignSession session, string planningResult)
        {
            var plan = new CampaignPlan
            {
                CampaignId = session.Campaign.Id,
                Context = $"Modern multi-agent campaign: '{session.Campaign.Goal}' with enhanced orchestration patterns",
                CreatedAt = DateTime.UtcNow,
                Steps = new List<PlanStep>()
            };

            // Step 1: Enhanced Industry Research with modern orchestration
            plan.Steps.Add(new PlanStep
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Enhanced Industry Research",
                Description = "Modern agent-powered industry analysis and company identification",
                AgentType = "ModernResearcherAgent",
                Function = "GetIndustryInsights",
                Parameters = new Dictionary<string, object>
                {
                    ["industry"] = session.Campaign.Audience,
                    ["enhancedAnalysis"] = true,
                    ["orchestrationReady"] = true
                }
            });

            // Step 2: Company Brief Generation with approval workflow
            plan.Steps.Add(new PlanStep
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Generate Company Briefs",
                Description = "Create comprehensive targeting briefs for each company using modern agent collaboration",
                AgentType = "ModernResearcherAgent",
                Function = "GenerateCompanyBriefs",
                Parameters = new Dictionary<string, object>
                {
                    ["campaignId"] = session.Campaign.Id,
                    ["collaborationMode"] = "GroupChat"
                },
                RequiresHumanApproval = true,
                ApprovalStatus = HumanApprovalStatus.PendingReview
            });

            // Step 3: Content Generation Orchestration
            plan.Steps.Add(new PlanStep
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Orchestrated Content Generation",
                Description = "Parallel content generation using modern orchestration patterns",
                AgentType = "ModernContentOrchestrator",
                Function = "OrchestateContentGeneration",
                Parameters = new Dictionary<string, object>
                {
                    ["campaignId"] = session.Campaign.Id,
                    ["components"] = session.Campaign.Components,
                    ["orchestrationPattern"] = "Concurrent"
                }
            });

            // Step 4: Final Campaign Deployment with modern coordination
            plan.Steps.Add(new PlanStep
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Modern Campaign Deployment",
                Description = "Enhanced deployment coordination using latest orchestration features",
                AgentType = "ModernDeploymentOrchestrator",
                Function = "CoordinateModernDeployment",
                Parameters = new Dictionary<string, object>
                {
                    ["campaignId"] = session.Campaign.Id,
                    ["orchestrationPattern"] = "Sequential",
                    ["humanOversight"] = true
                }
            });

            return plan;
        }

        private int EstimateExecutionTime(CampaignPlan plan)
        {
            // Enhanced estimation based on modern orchestration efficiency
            var baseTime = plan.Steps.Count * 3; // 3 minutes per step
            var approvalTime = plan.Steps.Count(s => s.RequiresHumanApproval) * 5; // 5 minutes per approval
            var orchestrationEfficiency = 0.7; // 30% efficiency gain from modern patterns
            
            return (int)((baseTime + approvalTime) * orchestrationEfficiency);
        }
    }
}
