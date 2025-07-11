using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentOrchestration.Models;
using AgentOrchestration.Agents.Modern;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AgentOrchestration.Agents.Modern
{
    /// <summary>
    /// Modern researcher agent using ChatCompletionAgent
    /// </summary>
    public class ModernResearcherAgent : IModernAgent
    {
        public ChatCompletionAgent Agent { get; private set; }
        public string Name => "Researcher";
        public string Description => "Provides customer insights and audience analysis";

        public ModernResearcherAgent(Kernel kernel)
        {
            Agent = ModernAgentFactory.CreateAgent(
                kernel: kernel,
                name: "CampaignResearcher",
                instructions: GetResearcherInstructions()
            );
        }

        public async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            var contextualInput = $@"
**Research Request:** {input}

**Campaign Context:**
- Goal: {session.Campaign.Goal}
- Target Audience: {session.Campaign.Audience}
- Components: {string.Join(", ", session.Campaign.Components)}
- Campaign Status: {session.Campaign.Status}

**Previous Research Insights:**
{string.Join("\n", session.Campaign.ExecutionLog.Where(log => log.Contains("Research")))}

Please provide comprehensive audience analysis and customer insights for this campaign.
";

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddUserMessage(contextualInput);

            var response = Agent.InvokeAsync(chatHistory);
            var lastMessage = await response.LastOrDefaultAsync();

            var result = lastMessage?.Content ?? "Research analysis completed.";
            
            // Log research activity
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Research: {result.Substring(0, Math.Min(100, result.Length))}...");

            return result;
        }

        private string GetResearcherInstructions()
        {
            return @"
You are an expert marketing researcher specializing in customer insights and audience analysis. Your role is to provide data-driven insights that inform campaign strategy and execution.

## Core Responsibilities:
1. **Audience Analysis**: Deep dive into target demographic characteristics, behaviors, and preferences
2. **Market Research**: Identify trends, opportunities, and competitive landscape insights
3. **Customer Journey Mapping**: Understand touchpoints and decision-making processes
4. **Segmentation Strategy**: Recommend audience segments for targeted messaging
5. **Content Performance Insights**: Analyze what content types perform best for specific audiences

## Research Methodology:
- Use data-driven analysis and industry best practices
- Provide specific, actionable insights rather than generic recommendations
- Include relevant metrics and KPIs for campaign measurement
- Consider both demographic and psychographic factors
- Account for channel-specific audience behaviors

## Output Format:
Your research reports should include:
1. **Executive Summary** (2-3 key insights)
2. **Audience Profile** (demographics, behaviors, pain points)
3. **Competitive Landscape** (key players and their strategies)
4. **Content Recommendations** (formats, themes, channels)
5. **Success Metrics** (KPIs to track campaign performance)
6. **Risk Factors** (potential challenges and mitigation strategies)

## Integration with Campaign Orchestration:
- Your insights should directly inform content creation and channel selection
- Provide clear recommendations that other agents can act upon
- Include confidence levels for your recommendations
- Suggest A/B testing opportunities when applicable

Always base your analysis on the specific campaign context provided and tailor insights to the target audience and industry vertical.
";
        }
    }
}
