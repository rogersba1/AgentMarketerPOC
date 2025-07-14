using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentOrchestration.Models;
using AgentOrchestration.Agents.Modern;
using AgentOrchestration.Services;
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

        private readonly MockCompanyDataService _companyDataService;

        public ModernResearcherAgent(Kernel kernel)
        {
            Agent = ModernAgentFactory.CreateAgent(
                kernel: kernel,
                name: "CampaignResearcher",
                instructions: GetResearcherInstructions()
            );
            
            _companyDataService = new MockCompanyDataService();
            // Initialize company data asynchronously
            _ = _companyDataService.LoadCompanyDataAsync();
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

        /// <summary>
        /// Generates a detailed company brief for targeting strategy based on research and campaign goals
        /// This is the critical method that was lost in modernization - restores human-in-the-loop workflow
        /// </summary>
        public async Task<string> GenerateCompanyBrief(string goal, string companyId, string insights = "")
        {
            await Task.Delay(700); // Simulate processing time for research synthesis

            var company = _companyDataService.GetCompanyById(companyId);
            var companyName = company?.BasicInfo.CompanyName ?? companyId;
            if (company == null)
            {
                return $@"# Company Brief: {companyName}

## Executive Summary
Company brief for {companyName} - Limited data available. Recommend additional research.

## Campaign Alignment
**Goal**: {goal}
**Target Company**: {companyName}
**Status**: Requires additional research

## Key Insights
- Company name: {companyName}
- Industry: Unknown
- Size: Unknown
- Location: Unknown

## Recommendations
- Additional research needed to develop comprehensive targeting strategy

## Recommended Approach
1. Conduct deeper company research
2. Identify key decision makers
3. Analyze current technology stack
4. Review competitive landscape

## Next Steps
- Gather additional company intelligence
- Develop personalized messaging strategy
- Create content calendar
- Define success metrics
";
            }

            // Generate comprehensive company brief using available data
            var brief = $@"# Company Brief: {company.BasicInfo.CompanyName}

## Executive Summary
{company.BasicInfo.CompanyName} is a {company.BasicInfo.Industry.ToLower()} company with {company.Leadership.Employees} employees, generating approximately {company.Metrics.AnnualGrowthRate} annual growth. This brief outlines our strategic approach for engaging with them in our ""{goal}"" campaign.

## Company Overview
**Company Name**: {company.BasicInfo.CompanyName}
**Industry**: {company.BasicInfo.Industry}
**Size**: {company.Leadership.Employees} employees
**Location**: {company.BasicInfo.Headquarters}
**Website**: {company.BasicInfo.Website}
**Founded**: {company.BasicInfo.Founded}

## Financial & Performance Metrics
- **Annual Growth Rate**: {company.Metrics.AnnualGrowthRate}
- **Customer Satisfaction**: {company.Metrics.CustomerSatisfactionScore}
- **Market Share**: {company.Metrics.MarketShare}
- **Active Clients**: {company.Metrics.ActiveClients}

## Campaign Alignment Analysis
**Our Goal**: {goal}
**Why {company.BasicInfo.CompanyName}**: 
- Strong growth trajectory ({company.Metrics.AnnualGrowthRate})
- {company.BasicInfo.Industry} industry alignment
- {company.Leadership.Employees} employees fits our target profile
- Established since {company.BasicInfo.Founded}

## Key Messaging Pillars
1. **Growth Enablement**: Position our solution as supporting their {company.Metrics.AnnualGrowthRate} growth trajectory
2. **Industry Expertise**: Leverage our {company.BasicInfo.Industry} sector knowledge
3. **Scalability**: Address needs of {company.Leadership.Employees}-person organization
4. **Innovation**: Align with their digital transformation goals

## Personalization Strategy
- **Landing Page**: Highlight {company.BasicInfo.Industry}-specific benefits and case studies
- **Email**: Reference their {company.BasicInfo.Headquarters} market and growth metrics
- **LinkedIn**: Engage with industry trends relevant to {company.BasicInfo.Industry}
- **Ads**: Target decision makers in {company.BasicInfo.Headquarters}

## Risk Assessment
- **Market Share**: {company.Metrics.MarketShare} - Market position analysis
- **Customer Satisfaction**: {company.Metrics.CustomerSatisfactionScore} - {(ParseNumericValue(company.Metrics.CustomerSatisfactionScore) >= 8 ? "Positive brand perception" : "May need careful positioning")}
- **Competition**: Assess competitive landscape in {company.BasicInfo.Industry}

## Success Metrics
- **Engagement Rate**: Target >15% email open rate
- **Conversion**: Aim for 2-3% landing page conversion
- **Follow-up**: Schedule demo within 2 weeks of campaign launch
- **Pipeline**: Generate qualified lead within 30 days

## Budget Allocation Recommendation
- **Content Creation**: 30%
- **Paid Advertising**: 40%
- **Personalization Tools**: 20%
- **Follow-up Activities**: 10%

## Timeline
- **Week 1**: Content creation and approval
- **Week 2**: Campaign launch and initial outreach
- **Week 3**: Follow-up and nurturing
- **Week 4**: Analysis and next steps

## Additional Research Insights
{insights}

---
*Brief generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*
*Campaign Goal: {goal}*
*Target: {company.BasicInfo.CompanyName} ({company.BasicInfo.Industry})*
";

            return brief;
        }

        private static double ParseNumericValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            
            // Remove common suffixes and prefixes
            var cleaned = value.Replace("%", "").Replace("$", "").Replace(",", "").Replace("M", "").Replace("B", "").Replace("K", "");
            
            if (double.TryParse(cleaned, out double result))
            {
                return result;
            }
            
            return 0;
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
