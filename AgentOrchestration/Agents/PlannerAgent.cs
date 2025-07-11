using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using AgentOrchestration.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents
{
    /// <summary>
    /// Planner agent responsible for creating campaign execution plans and managing multi-company execution
    /// </summary>
    public class PlannerAgent : BaseAgent
    {
        private readonly MockCompanyDataService _companyDataService;

        public override string Name => "Planner";
        public override string Description => "Creates structured campaign execution plans and manages multi-company campaign execution";

        private const string PLANNER_SYSTEM_PROMPT = @"
You are a Marketing Campaign Planner AI. Your role is to create detailed, structured campaign execution plans based on user input.

When a user provides campaign requirements, you should:
1. Analyze the campaign goal and objectives
2. Identify target companies/audience (e.g., ""top 10 retail customers"", ""manufacturing companies"")
3. Determine what content components to generate (landing pages, emails, ads, etc.)
4. Create an execution plan that runs against the specified companies
5. Structure the plan for sequential execution

Available content generation capabilities:
- Landing Pages: Personalized HTML landing pages
- Email Campaigns: Personalized email drafts
- LinkedIn Posts: Industry-specific social media content
- Ad Copy: Multi-platform advertising content

Your response should focus on understanding:
- Campaign Goal: What is the marketing objective?
- Target Companies: Which companies should be targeted? (industry, count, criteria)
- Content Components: What marketing materials should be generated?
- Execution Strategy: How should the campaign be deployed?

Create actionable plans that leverage company-specific data for personalization.
";

        public PlannerAgent(Kernel kernel, MockCompanyDataService companyDataService) : base(kernel, PLANNER_SYSTEM_PROMPT)
        {
            _companyDataService = companyDataService;
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Planner: Processing campaign plan request");

                // Parse the campaign requirements from user input
                var campaignRequirements = await ParseCampaignRequirements(input, session.Campaign);
                
                // Identify target companies based on user criteria
                var targetCompanies = await IdentifyTargetCompanies(campaignRequirements);
                
                // Create execution plan
                var plan = CreateExecutionPlan(session.Campaign, campaignRequirements, targetCompanies);
                
                session.Plan = plan;
                session.Campaign.Status = CampaignStatus.InProgress;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Planner: Created execution plan with {plan.Steps.Count} steps for {targetCompanies.Count} companies");

                return FormatPlanResponse(plan, targetCompanies, campaignRequirements);
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Planner: Error - {ex.Message}");
                return $"Error creating campaign plan: {ex.Message}";
            }
        }

        private async Task<CampaignRequirements> ParseCampaignRequirements(string input, Campaign campaign)
        {
            // Use LLM to parse the user input and extract campaign requirements
            var parsePrompt = $@"
Parse the following campaign request and extract the key information:

User Input: {input}
Existing Campaign Goal: {campaign.Goal}
Existing Audience: {campaign.Audience}
Existing Components: {string.Join(", ", campaign.Components)}

Extract and return the following information:
1. Campaign Goal/Objective
2. Target Companies (industry, count, specific criteria like 'top 10 retail customers')
3. Content Components to generate (landing pages, emails, ads, etc.)
4. Any specific requirements or constraints

Provide a clear, structured response.
";

            var llmResponse = await CallLLMAsync(parsePrompt, input);

            // For now, create a structured requirements object
            // In a full implementation, you'd parse the LLM response more sophisticatedly
            return new CampaignRequirements
            {
                Goal = campaign.Goal,
                TargetCriteria = ExtractTargetCriteria(input, campaign.Audience),
                Components = campaign.Components.ToList(),
                SpecialInstructions = input
            };
        }

        private async Task<List<CompanyProfile>> IdentifyTargetCompanies(CampaignRequirements requirements)
        {
            // Ensure company data is loaded
            await _companyDataService.LoadCompanyDataAsync();

            var allCompanies = _companyDataService.GetAllCompanies();
            var targetCompanies = new List<CompanyProfile>();

            // Parse the target criteria
            var criteria = requirements.TargetCriteria;
            
            if (criteria.Industry.ToLower().Contains("retail"))
            {
                targetCompanies = _companyDataService.GetCompaniesByIndustry("retail");
            }
            else if (criteria.Industry.ToLower().Contains("manufacturing"))
            {
                targetCompanies = _companyDataService.GetCompaniesByIndustry("manufacturing");
            }
            else if (criteria.Industry.ToLower() == "all" || string.IsNullOrEmpty(criteria.Industry))
            {
                targetCompanies = allCompanies;
            }

            // Apply count limitation
            if (criteria.Count > 0 && criteria.Count < targetCompanies.Count)
            {
                // For "top N" requests, sort by revenue or growth rate
                targetCompanies = targetCompanies
                    .OrderByDescending(c => ParseNumericValue(c.Metrics.AnnualGrowthRate))
                    .ThenByDescending(c => ParseNumericValue(c.Metrics.CustomerSatisfactionScore))
                    .Take(criteria.Count)
                    .ToList();
            }

            return targetCompanies;
        }

        private TargetCriteria ExtractTargetCriteria(string input, string existingAudience)
        {
            var criteria = new TargetCriteria();
            var inputLower = input.ToLower();
            var audienceLower = existingAudience.ToLower();
            
            // Combine input and existing audience for analysis
            var combinedText = $"{inputLower} {audienceLower}";

            // Extract industry
            if (combinedText.Contains("retail"))
                criteria.Industry = "retail";
            else if (combinedText.Contains("manufacturing"))
                criteria.Industry = "manufacturing";
            else
                criteria.Industry = "all";

            // Extract count - check both input and existing audience
            var numbers = System.Text.RegularExpressions.Regex.Matches(combinedText, @"\d+");
            if (numbers.Count > 0)
            {
                if (int.TryParse(numbers[0].Value, out int count))
                    criteria.Count = count;
            }

            // Default to 10 if "top" is mentioned but no number found
            if (combinedText.Contains("top") && criteria.Count == 0)
                criteria.Count = 10;
                
            // If still no count and we have customers/companies mentioned, default to 20
            if (criteria.Count == 0 && (combinedText.Contains("customer") || combinedText.Contains("compan")))
                criteria.Count = 20;

            return criteria;
        }

        private CampaignPlan CreateExecutionPlan(Campaign campaign, CampaignRequirements requirements, List<CompanyProfile> targetCompanies)
        {
            var plan = new CampaignPlan
            {
                CampaignId = campaign.Id,
                Context = $"Multi-company campaign: '{requirements.Goal}' targeting {targetCompanies.Count} {requirements.TargetCriteria.Industry} companies with individual execution per company"
            };


            // Step 2-N: Individual company execution
            // For each target company, create individual steps for each component
            int stepCounter = 1;
            foreach (var company in targetCompanies)
            {
                // Step: Generate company brief (requires human approval)
                plan.Steps.Add(new PlanStep
                {
                    Name = $"Generate Company Brief for {company.BasicInfo.CompanyName}",
                    Description = $"Create a comprehensive targeting brief for {company.BasicInfo.CompanyName} including strategy, messaging, and personalization approach",
                    AgentType = "ResearcherAgent",
                    Function = "GenerateCompanyBrief",
                    Parameters = new Dictionary<string, object>
                    {
                        { "goal", requirements.Goal },
                        { "companyName", company.BasicInfo.CompanyName },
                        { "insights", $"Company-specific research data for {company.BasicInfo.CompanyName}" }
                    },
                    RequiresHumanApproval = true,
                    ApprovalStatus = HumanApprovalStatus.PendingReview
                });

                // Step: Review company brief (human-in-the-loop)
                plan.Steps.Add(new PlanStep
                {
                    Name = $"Review Company Brief for {company.BasicInfo.CompanyName}",
                    Description = $"Human review and approval of the generated company brief for {company.BasicInfo.CompanyName}",
                    AgentType = "RouterAgent",
                    Function = "ReviewCompanyBrief",
                    Parameters = new Dictionary<string, object>
                    {
                        { "companyName", company.BasicInfo.CompanyName },
                        { "companyId", company.CompanyId }
                    },
                    RequiresHumanApproval = true,
                    ApprovalStatus = HumanApprovalStatus.PendingReview
                });

                // For each content component, create individual company-specific steps
                foreach (var component in requirements.Components)
                {
                    var stepName = $"Generate {component} for {company.BasicInfo.CompanyName}";
                    var function = GetPersonalizedFunction(component);

                    plan.Steps.Add(new PlanStep
                    {
                        Name = stepName,
                        Description = $"Create personalized {component} content specifically for {company.BasicInfo.CompanyName} ({company.BasicInfo.Industry})",
                        AgentType = "ContentTool",
                        Function = function,
                        Parameters = new Dictionary<string, object>
                        {
                            { "goal", requirements.Goal },
                            { "companyName", company.BasicInfo.CompanyName },
                            { "insights", $"Company-specific data for {company.BasicInfo.CompanyName}" }
                        }
                    });
                }

                // Add a company-specific deployment step
                plan.Steps.Add(new PlanStep
                {
                    Name = $"Deploy {company.BasicInfo.CompanyName} Campaign",
                    Description = $"Deploy all generated content for {company.BasicInfo.CompanyName} and activate the campaign",
                    AgentType = "RouterAgent",
                    Function = "ReviewCompanyCampaign",
                    Parameters = new Dictionary<string, object>
                    {
                        { "campaignId", campaign.Id },
                        { "companyName", company.BasicInfo.CompanyName },
                        { "companyId", company.CompanyId }
                    }
                });

                stepCounter++;
            }

            // Final step: Overall campaign deployment and coordination
            plan.Steps.Add(new PlanStep
            {
                Name = "Final Campaign Deployment",
                Description = $"Complete deployment coordination for all {targetCompanies.Count} companies and launch the campaign",
                AgentType = "RouterAgent",
                Function = "CoordinateMultiCompanyCampaign",
                Parameters = new Dictionary<string, object>
                {
                    { "campaignId", campaign.Id },
                    { "totalCompanies", targetCompanies.Count },
                    { "targetCompanies", targetCompanies.Select(c => c.BasicInfo.CompanyName).ToList() }
                }
            });

            return plan;
        }

        private string GetPersonalizedFunction(string component)
        {
            return component.ToLower() switch
            {
                "landing site" or "landing page" => "generate_personalized_landing_page",
                "email" => "generate_personalized_email", 
                "linkedin" or "linkedin post" => "generate_personalized_linkedin_post",
                "ads" or "ad copy" => "generate_personalized_ad_copy",
                _ => "generate_personalized_content"
            };
        }

        private string FormatPlanResponse(CampaignPlan plan, List<CompanyProfile> targetCompanies, CampaignRequirements requirements)
        {
            var response = $@"
🎯 **Individual Company Campaign Execution Plan Created**

**Campaign Overview:**
- Goal: {requirements.Goal}
- Target: {targetCompanies.Count} {requirements.TargetCriteria.Industry} companies
- Components: {string.Join(", ", requirements.Components)}
- Strategy: Individual execution per company for maximum personalization

**Target Companies:**
{string.Join("\n", targetCompanies.Take(5).Select(c => $"• {c.BasicInfo.CompanyName} ({c.BasicInfo.Industry}) - {c.Leadership.Employees} employees"))}
{(targetCompanies.Count > 5 ? $"... and {targetCompanies.Count - 5} more companies" : "")}

**Execution Strategy:**
Each company will receive individually crafted content and immediate deployment:
- Personalized research and insights gathering
- Custom {string.Join(", ", requirements.Components)} for each company
- Individual deployment workflow
- Company-specific campaign activation

**Total Execution Steps: {plan.Steps.Count}**
1. Industry Research (1 step)
2. Individual Company Execution ({targetCompanies.Count} companies × {requirements.Components.Count + 2} steps each = {targetCompanies.Count * (requirements.Components.Count + 2)} steps)
3. Final Deployment (1 step)

**Sample Execution Flow for {targetCompanies.FirstOrDefault()?.BasicInfo.CompanyName ?? "First Company"}:**
{string.Join("\n", plan.Steps.Where(s => s.Name.Contains(targetCompanies.FirstOrDefault()?.BasicInfo.CompanyName ?? "")).Take(3).Select((s, i) => $"• {s.Name}: {s.Description}"))}

Ready to execute this personalized multi-company campaign!
";

            return response;
        }

        private double ParseNumericValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            
            // Remove % and other non-numeric characters, then parse
            var numericString = System.Text.RegularExpressions.Regex.Replace(value, @"[^\d.]", "");
            return double.TryParse(numericString, out double result) ? result : 0;
        }

    }
}
