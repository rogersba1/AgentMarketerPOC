using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentOrchestration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AgentMarketer.Tests.Helpers
{
    /// <summary>
    /// Intelligent mock LLM service that provides realistic responses for testing
    /// without requiring actual Azure Foundry API calls
    /// </summary>
    public class MockLLMService : IChatCompletionService
    {
        private readonly Dictionary<string, Func<string, string>> _responsePatterns;

        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public MockLLMService()
        {
            _responsePatterns = InitializeResponsePatterns();
        }

        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            var lastMessage = chatHistory.LastOrDefault()?.Content ?? "";
            var response = GenerateIntelligentResponse(lastMessage);

            var messageContent = new ChatMessageContent(
                AuthorRole.Assistant,
                response
            );

            var result = new List<ChatMessageContent> { messageContent };
            return Task.FromResult<IReadOnlyList<ChatMessageContent>>(result);
        }

        public IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory,
            PromptExecutionSettings? executionSettings = null,
            Kernel? kernel = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("Streaming not implemented for mock service");
        }

        private string GenerateIntelligentResponse(string input)
        {
            var inputLower = input.ToLowerInvariant();

            // Campaign request parsing responses
            if (inputLower.Contains("parse") && inputLower.Contains("campaign"))
            {
                return GenerateParsedCampaignResponse(input);
            }

            // Company brief generation responses
            if (inputLower.Contains("company brief") || inputLower.Contains("research"))
            {
                return GenerateCompanyBriefResponse(input);
            }

            // Content generation responses
            if (inputLower.Contains("content") || inputLower.Contains("landing page") || 
                inputLower.Contains("email") || inputLower.Contains("linkedin"))
            {
                return GenerateContentResponse(input);
            }

            // Execution planning responses
            if (inputLower.Contains("execution plan") || inputLower.Contains("planning"))
            {
                return GenerateExecutionPlanResponse(input);
            }

            // Default intelligent response
            return GenerateDefaultResponse(input);
        }

        private string GenerateParsedCampaignResponse(string input)
        {
            // Extract audience hints from input
            var audience = "retail";
            if (input.Contains("manufacturing")) audience = "manufacturing";
            else if (input.Contains("technology")) audience = "technology";
            else if (input.Contains("healthcare")) audience = "healthcare";
            else if (input.Contains("finance")) audience = "finance";

            // Extract company count
            var companyCount = 3;
            var numbers = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            foreach (var num in numbers)
            {
                if (input.Contains(num))
                {
                    companyCount = int.Parse(num);
                    break;
                }
            }

            // Extract content components
            var components = new List<string>();
            if (input.Contains("landing")) components.Add("landing_page");
            if (input.Contains("email")) components.Add("email");
            if (input.Contains("linkedin")) components.Add("linkedin_post");
            if (input.Contains("ad")) components.Add("ad_copy");
            
            if (components.Count == 0)
            {
                components.AddRange(new[] { "email", "linkedin_post" }); // defaults
            }

            return $@"Based on the campaign request analysis:

**Parsed Campaign Parameters:**
- Target Audience: {audience}
- Company Count: {companyCount}
- Content Components: {string.Join(", ", components)}
- Additional Context: Mock test scenario
- Confidence Score: 0.95

**Execution Plan:**
1. Research Phase: Identify {companyCount} companies in {audience} industry
2. Brief Generation: Create detailed company profiles and market analysis
3. Content Creation: Generate {string.Join(", ", components)} for each company
4. Approval Workflow: Human review before content generation phase

This plan will coordinate between ResearchAgent and ContentAgent to deliver comprehensive marketing materials.";
        }

        private string GenerateCompanyBriefResponse(string input)
        {
            var companyName = ExtractCompanyName(input) ?? "TechCorp Solutions";
            
            return $@"## Company Brief: {companyName}

**Company Overview:**
{companyName} is a leading organization in their industry sector, demonstrating strong market presence and growth potential.

**Key Business Metrics:**
- Industry Position: Market leader in their segment
- Target Market: Enterprise and mid-market customers
- Revenue Range: $50M-$500M annually
- Employee Count: 200-1,000 employees

**Marketing Insights:**
- Primary Challenges: Digital transformation, customer acquisition
- Key Decision Makers: C-suite executives, VP Marketing, IT Directors
- Preferred Communication: Professional, data-driven messaging
- Best Engagement Channels: LinkedIn, email, industry publications

**Campaign Recommendations:**
- Focus on ROI and efficiency messaging
- Highlight industry-specific solutions
- Emphasize proven track record and customer success
- Use professional tone with technical depth

This brief provides the foundation for personalized content creation.";
        }

        private string GenerateContentResponse(string input)
        {
            var contentType = ExtractContentType(input);
            var companyName = ExtractCompanyName(input) ?? "TechCorp Solutions";

            return contentType.ToLower() switch
            {
                "landing_page" => GenerateLandingPageContent(companyName),
                "email" => GenerateEmailContent(companyName),
                "linkedin_post" => GenerateLinkedInContent(companyName),
                "ad_copy" => GenerateAdCopyContent(companyName),
                _ => GenerateDefaultContent(companyName)
            };
        }

        private string GenerateLandingPageContent(string companyName)
        {
            return $@"# Personalized Landing Page for {companyName}

## Hero Section
**Transform Your Business with Cutting-Edge Solutions**
Tailored specifically for {companyName}'s industry challenges and growth objectives.

## Value Proposition
- ✅ Increase operational efficiency by 40%
- ✅ Reduce costs while scaling operations
- ✅ Industry-proven solutions with guaranteed ROI

## Call to Action
[Schedule Your Personalized Demo] - Exclusive for {companyName}

*Landing page optimized for {companyName}'s target audience and business goals.*";
        }

        private string GenerateEmailContent(string companyName)
        {
            return $@"Subject: Exclusive Partnership Opportunity for {companyName}

Dear {companyName} Leadership Team,

I hope this email finds you well. I'm reaching out because of {companyName}'s impressive growth and industry leadership.

**Why This Matters for {companyName}:**
Our solutions have helped similar organizations achieve:
• 40% improvement in operational efficiency  
• 25% reduction in operational costs
• Faster time-to-market for new initiatives

**Next Steps:**
I'd love to explore how we can support {companyName}'s continued success. Would you be available for a brief 15-minute conversation next week?

Best regards,
[Your Name]

*This email has been personalized for {companyName} based on industry research and company analysis.*";
        }

        private string GenerateLinkedInContent(string companyName)
        {
            return $@"🚀 Congratulations to {companyName} on their continued industry leadership!

It's inspiring to see companies like {companyName} driving innovation in their sector. Their commitment to excellence aligns perfectly with the solutions we provide to industry leaders.

For organizations like {companyName} looking to:
✅ Scale operations efficiently
✅ Reduce operational complexity  
✅ Accelerate digital transformation

We've developed proven strategies that deliver measurable results.

{companyName} team - would love to connect and explore how we can support your next growth phase.

#Innovation #BusinessGrowth #DigitalTransformation

*LinkedIn post tailored for {companyName}'s network and industry positioning.*";
        }

        private string GenerateAdCopyContent(string companyName)
        {
            return $@"**Headline:** Exclusive Solutions for {companyName} - Limited Time Offer

**Body Copy:**
{companyName}, you're invited to discover how industry leaders are achieving 40% efficiency gains.

✅ Proven ROI in 90 days
✅ Industry-specific implementation
✅ Dedicated support team

**Call to Action:** Get Your {companyName} Assessment - Free

*Ad copy optimized for {companyName}'s target demographics and business objectives.*";
        }

        private string GenerateDefaultContent(string companyName)
        {
            return $@"**Personalized Marketing Content for {companyName}**

This content has been specifically created for {companyName} based on:
- Industry analysis and market positioning
- Company size and growth trajectory  
- Target audience preferences
- Competitive landscape assessment

Content includes strategic messaging, value propositions, and calls-to-action optimized for {companyName}'s unique business environment.

*Generated using intelligent content personalization algorithms.*";
        }

        private string GenerateExecutionPlanResponse(string input)
        {
            return @"## Campaign Execution Plan

**Phase 1: Research & Planning (Days 1-3)**
1. Industry analysis and company identification
2. Competitive landscape assessment
3. Target audience profiling
4. Message framework development

**Phase 2: Content Creation (Days 4-7)**  
1. Company brief generation for each target
2. Personalized content development
3. Quality assurance and brand alignment
4. Approval workflow preparation

**Phase 3: Review & Approval (Days 8-9)**
1. Human review of generated content
2. Feedback incorporation and revisions
3. Final approval and sign-off
4. Campaign deployment preparation

**Phase 4: Deployment (Day 10)**
1. Content distribution across channels
2. Performance tracking setup
3. Initial engagement monitoring
4. Success metrics reporting

This execution plan ensures comprehensive campaign delivery with quality checkpoints throughout the process.";
        }

        private string GenerateDefaultResponse(string input)
        {
            return @"I understand your request and I'm ready to help with campaign planning and execution. 

As the Campaign Planner Agent, I can assist with:
- Parsing campaign requirements from natural language
- Creating structured execution plans
- Coordinating with research and content generation
- Setting up approval workflows

Please provide your campaign requirements, and I'll create a detailed plan for execution.";
        }

        private string ExtractCompanyName(string input)
        {
            // Look for common patterns that indicate company names
            var patterns = new[]
            {
                @"for\s+([A-Za-z][A-Za-z\s]+?)(?:\s+in\s|\s+with\s|\s*$|\s+industry)",
                @"Generate.*?for\s+([A-Za-z][A-Za-z\s]+?)(?:\s+in\s|\s+with\s|\s*$)",
                @"company\s+brief\s+for\s+([A-Za-z][A-Za-z\s]+?)(?:\s+in\s|\s*$)",
                @"content\s+for\s+([A-Za-z][A-Za-z\s]+?)(?:\s+in\s|\s*$)",
                @"([A-Za-z][A-Za-z\s]*(?:Corp|Corporation|Inc|Ltd|Solutions|Tech|Company|Dynamics|Labs|Systems|Group))"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var companyName = match.Groups[1].Value.Trim();
                    if (companyName.Length > 2 && companyName.Length < 50)
                    {
                        return companyName;
                    }
                }
            }

            // If no specific pattern matches, try to find company-like words
            var words = input.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length - 1; i++)
            {
                if (words[i].EndsWith("Corp", System.StringComparison.OrdinalIgnoreCase) ||
                    words[i].EndsWith("Inc", System.StringComparison.OrdinalIgnoreCase) ||
                    words[i].Contains("Tech", System.StringComparison.OrdinalIgnoreCase) ||
                    words[i].Contains("Solutions", System.StringComparison.OrdinalIgnoreCase))
                {
                    // Return the word plus one preceding word if it exists
                    if (i > 0)
                        return $"{words[i - 1]} {words[i]}";
                    return words[i];
                }
            }

            return "TechCorp Solutions"; // Default fallback
        }

        private string ExtractContentType(string input)
        {
            if (input.Contains("landing")) return "landing_page";
            if (input.Contains("email")) return "email";
            if (input.Contains("linkedin")) return "linkedin_post";
            if (input.Contains("ad")) return "ad_copy";
            return "email"; // default
        }

        private Dictionary<string, Func<string, string>> InitializeResponsePatterns()
        {
            return new Dictionary<string, Func<string, string>>
            {
                ["campaign_parsing"] = GenerateParsedCampaignResponse,
                ["company_brief"] = GenerateCompanyBriefResponse,
                ["content_generation"] = GenerateContentResponse,
                ["execution_planning"] = GenerateExecutionPlanResponse
            };
        }
    }
}
