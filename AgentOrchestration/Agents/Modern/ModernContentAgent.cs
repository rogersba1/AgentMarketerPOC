using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentOrchestration.Models;
using AgentOrchestration.Services;
using AgentOrchestration.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents.Modern
{
    /// <summary>
    /// ContentAgent: Generates marketing content using ContentGenerationTools
    /// Responsibilities:
    /// - Execute content generation for approved companies
    /// - Use ContentGenerationTools (landing_page, email, linkedin_post, ad_copy)
    /// - Coordinate content creation based on planned components
    /// - Report content generation results
    /// </summary>
    public class ModernContentAgent
    {
        private readonly ContentGenerationTools _contentTools;
        public ChatCompletionAgent Agent { get; private set; }

        public ModernContentAgent(Kernel kernel, MockCompanyDataService companyDataService)
        {
            _contentTools = new ContentGenerationTools(kernel, companyDataService);
            Agent = CreateAgent(kernel);
        }

        private ChatCompletionAgent CreateAgent(Kernel kernel)
        {
            return new ChatCompletionAgent()
            {
                Instructions = @"
You are the Content Agent responsible for generating marketing content using specialized tools.

**Your Core Responsibilities:**
1. **Content Generation**: Execute content creation for approved companies using:
   - generate_personalized_landing_page: HTML landing pages
   - generate_personalized_email: Email campaigns
   - generate_personalized_linkedin_post: LinkedIn social content
   - generate_personalized_ad_copy: Advertisement copy

2. **Execution Coordination**: Based on planned components:
   - Generate content for each approved company
   - Use company briefs to personalize content
   - Follow the campaign goal and target audience
   - Report generation progress and results

3. **Quality Assurance**: Ensure content is:
   - Personalized for each target company
   - Aligned with campaign objectives
   - Appropriate for the target audience
   - Professional and engaging

**Content Generation Process:**
1. Receive approved companies and their briefs
2. For each company, generate the planned content components
3. Use ContentGenerationTools for actual generation
4. Report results and any issues

**Available Tools:**
- Landing Page: Full HTML pages with company personalization
- Email: Personalized email campaigns with company-specific messaging
- LinkedIn Post: Social media content optimized for LinkedIn
- Ad Copy: Advertisement text and creative copy

Remember: Content quality depends on using company briefs effectively for personalization.
",
                Name = "ContentAgent",
                Kernel = kernel
            };
        }

        /// <summary>
        /// Generate content for approved companies based on planned components
        /// </summary>
        public async Task<ContentGenerationResult> GenerateContentAsync(
            List<CampaignCompany> approvedCompanies, 
            List<string> components, 
            string campaignGoal)
        {
            var results = new List<CompanyContentResult>();
            var totalTasks = approvedCompanies.Count * components.Count;
            var completedTasks = 0;

            foreach (var company in approvedCompanies)
            {
                var companyResults = new List<ContentItemResult>();

                foreach (var component in components)
                {
                    try
                    {
                        var content = await GenerateContentForComponent(
                            company.CompanyId, 
                            component, 
                            company.Brief, 
                            campaignGoal);

                        companyResults.Add(new ContentItemResult
                        {
                            Component = component,
                            Content = content,
                            Status = "Success",
                            GeneratedAt = DateTime.UtcNow
                        });

                        completedTasks++;
                    }
                    catch (Exception ex)
                    {
                        companyResults.Add(new ContentItemResult
                        {
                            Component = component,
                            Content = "",
                            Status = $"Failed: {ex.Message}",
                            GeneratedAt = DateTime.UtcNow
                        });

                        completedTasks++;
                    }
                }

                results.Add(new CompanyContentResult
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.CompanyName,
                    ContentItems = companyResults
                });
            }

            return new ContentGenerationResult
            {
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                SuccessfulTasks = results.SelectMany(r => r.ContentItems).Count(c => c.Status == "Success"),
                CompanyResults = results,
                GeneratedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Generate content for a specific component using ContentGenerationTools
        /// </summary>
        private async Task<string> GenerateContentForComponent(
            string companyId, 
            string component, 
            string brief, 
            string campaignGoal)
        {
            return component.ToLower() switch
            {
                "landing_page" => await _contentTools.GeneratePersonalizedLandingPage(campaignGoal, companyId, brief),
                "email" => await _contentTools.GeneratePersonalizedEmail(campaignGoal, companyId, brief),
                "linkedin_post" => await _contentTools.GeneratePersonalizedLinkedInPost(campaignGoal, companyId, brief),
                "ad_copy" => await _contentTools.GeneratePersonalizedAdCopy(campaignGoal, companyId, brief),
                _ => throw new ArgumentException($"Unknown content component: {component}")
            };
        }

        /// <summary>
        /// Get available content generation tools/components
        /// </summary>
        public static List<string> GetAvailableComponents()
        {
            return new List<string> { "landing_page", "email", "linkedin_post", "ad_copy" };
        }
    }

    // Supporting data models for ContentAgent
    public class ContentGenerationResult
    {
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int SuccessfulTasks { get; set; }
        public List<CompanyContentResult> CompanyResults { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }

    public class CompanyContentResult
    {
        public string CompanyId { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public List<ContentItemResult> ContentItems { get; set; } = new();
    }

    public class ContentItemResult
    {
        public string Component { get; set; } = "";
        public string Content { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
    }
}
