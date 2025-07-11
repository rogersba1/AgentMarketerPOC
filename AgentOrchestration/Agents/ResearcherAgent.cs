using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using AgentOrchestration.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents
{
    /// <summary>
    /// Researcher agent that generates detailed company-specific briefs for targeted marketing campaigns
    /// </summary>
    public class ResearcherAgent : BaseAgent
    {
        public override string Name => "Researcher";
        public override string Description => "Generates detailed company briefs and targeting strategies";

        private const string RESEARCHER_SYSTEM_PROMPT = @"
You are a Company Research and Brief Generation AI. Your role is to analyze individual companies and create detailed targeting briefs for marketing campaigns.

When provided with a company and campaign goal, you should:
1. Analyze the company's profile, industry, and market position
2. Identify key decision makers and stakeholders  
3. Assess technology needs and growth opportunities
4. Develop personalized messaging strategies
5. Create a comprehensive brief with targeting recommendations

Focus on delivering actionable, company-specific insights that enable highly targeted and personalized marketing campaigns. Each brief should be thorough enough to guide all subsequent content creation for that company.
";

        //private readonly List<Customer> _mockCustomerData;
        private readonly MockCompanyDataService _companyDataService;

        public ResearcherAgent(Kernel kernel) : base(kernel, RESEARCHER_SYSTEM_PROMPT)
        {
            //_mockCustomerData = InitializeMockCustomerData();
            _companyDataService = new MockCompanyDataService();
            
            // Initialize company data asynchronously
            _ = _companyDataService.LoadCompanyDataAsync();
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Processing company brief request");

                // Parse the input to extract company name and campaign goal
                var companyName = ExtractCompanyName(input);
                var goal = session.Campaign.Goal ?? "Drive engagement and growth";

                if (string.IsNullOrEmpty(companyName))
                {
                    return "Error: Could not identify company name from input. Please specify the target company.";
                }

                // Generate the company brief
                var brief = await GenerateCompanyBrief(goal, companyName);
                
                // Store the brief using the new CampaignCompany structure
                StoreCompanyBrief(session, companyName, brief);

                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Generated company brief for {companyName}");

                return brief;
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Error - {ex.Message}");
                return $"Error generating company brief: {ex.Message}";
            }
        }

        /// <summary>
        /// Stores company brief for a specific company in the campaign
        /// </summary>
        private void StoreCompanyBrief(CampaignSession session, string companyName, string brief)
        {
            var companyId = companyName; // Use company name as ID if no specific ID available
            var campaignCompany = session.Campaign.Companies.FirstOrDefault(c => c.CompanyId == companyId || c.CompanyName == companyName);
            
            if (campaignCompany == null)
            {
                campaignCompany = new CampaignCompany
                {
                    CompanyId = companyId,
                    CompanyName = companyName,
                    CreatedAt = DateTime.UtcNow
                };
                session.Campaign.Companies.Add(campaignCompany);
            }
            
            campaignCompany.Brief = brief;
            campaignCompany.LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Extracts company name from natural language input
        /// </summary>
        private string ExtractCompanyName(string input)
        {
            // Simple extraction logic - can be enhanced with more sophisticated parsing
            var parts = input.Split(new[] { "for", "targeting", "company" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return parts[1].Trim().Split(' ').Take(3).Aggregate((a, b) => a + " " + b);
            }
            
            // Fallback: look for company names in the input
            return input.Trim();
        }

        /// <summary>
        /// Generates a detailed company brief for targeting strategy based on research and campaign goals
        /// </summary>
        public async Task<string> GenerateCompanyBrief(string goal, string companyName, string insights = "")
        {
            await Task.Delay(700); // Simulate processing time for research synthesis

            var company = _companyDataService.GetCompanyByName(companyName);
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
    }
}
