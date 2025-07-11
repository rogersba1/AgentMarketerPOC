using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AgentOrchestration.Services
{
    /// <summary>
    /// Service for parsing natural language input into campaign components
    /// </summary>
    public class CampaignParsingService
    {
        private readonly Kernel _kernel;

        public CampaignParsingService(Kernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Parses natural language input to extract campaign goal, audience, and components
        /// </summary>
        public async Task<ParsedCampaign> ParseCampaignInputAsync(string naturalLanguageInput)
        {
            var parsed = new ParsedCampaign();

            // Use both rule-based and AI-powered parsing
            var ruleBasedResult = ParseWithRules(naturalLanguageInput);
            var aiEnhancedResult = await EnhanceWithAI(naturalLanguageInput, ruleBasedResult);

            return aiEnhancedResult;
        }

        private ParsedCampaign ParseWithRules(string input)
        {
            var parsed = new ParsedCampaign();
            var inputLower = input.ToLower();

            // Extract goal/objective
            parsed.Goal = ExtractGoal(input);

            // Extract audience
            parsed.Audience = ExtractAudience(input);

            // Extract components
            parsed.Components = ExtractComponents(input);

            return parsed;
        }

        private string ExtractGoal(string input)
        {
            var inputLower = input.ToLower();
            
            // Look for industry-specific goals that match our mock data
            if (inputLower.Contains("retail") || inputLower.Contains("shopping") || inputLower.Contains("consumer"))
            {
                if (inputLower.Contains("ai") || inputLower.Contains("technology") || inputLower.Contains("digital"))
                {
                    return "Promote AI-driven customer experience and personalization solutions for retail companies";
                }
                return "Drive customer engagement and sales growth for retail companies";
            }
            
            if (inputLower.Contains("manufactur") || inputLower.Contains("industrial") || inputLower.Contains("production"))
            {
                if (inputLower.Contains("ai") || inputLower.Contains("automation") || inputLower.Contains("digital"))
                {
                    return "Promote AI-powered automation and operational efficiency solutions for manufacturing companies";
                }
                return "Showcase operational efficiency and cost reduction solutions for manufacturing companies";
            }
            
            // AI/Technology focused goals
            if (inputLower.Contains("ai") || inputLower.Contains("artificial intelligence"))
            {
                if (inputLower.Contains("solution") || inputLower.Contains("software") || inputLower.Contains("product"))
                {
                    return "Promote AI solutions and their business benefits across multiple industries";
                }
                return "Highlight AI capabilities and transformation opportunities";
            }
            
            // Look for patterns that indicate campaign goals
            var goalPatterns = new[]
            {
                @"(?:to |campaign to |want to |like to |goal is to |objective is to |aim to |focus on |highlight |promote |showcase |advertise |market )([^.]+?)(?:\s+for|\s+to|\s+with|\s+that|\s*$)",
                @"(?:create a campaign )([^.]+?)(?:\s+for|\s+to|\s+with|\s+that|\s*$)",
                @"(?:about |regarding |concerning )([^.]+?)(?:\s+for|\s+to|\s+with|\s+that|\s*$)"
            };

            foreach (var pattern in goalPatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var goal = match.Groups[1].Value.Trim();
                    // Clean up common prefixes
                    goal = Regex.Replace(goal, @"^(create a campaign|campaign|to|that)", "", RegexOptions.IgnoreCase).Trim();
                    if (!string.IsNullOrEmpty(goal))
                    {
                        return goal;
                    }
                }
            }

            // Fallback: look for key topics/subjects that match our capabilities
            var topicPatterns = new[]
            {
                @"(manufacturing automation|digital transformation|ai capabilities|marketing automation|cloud solutions|data analytics|cybersecurity|software development|product launch|brand awareness|customer experience|operational efficiency)",
                @"(automation|ai|marketing|sales|productivity|efficiency|growth|innovation|technology|digital|retail|manufacturing)"
            };

            foreach (var pattern in topicPatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var topic = match.Groups[1].Value;
                    return $"Promote {topic} benefits and business value";
                }
            }

            return "Drive business growth and technology adoption";
        }

        private string ExtractAudience(string input)
        {
            var inputLower = input.ToLower();
            
            // First, look for our specific mock data industries
            if (inputLower.Contains("retail") || inputLower.Contains("shopping") || inputLower.Contains("consumer") || 
                inputLower.Contains("store") || inputLower.Contains("ecommerce") || inputLower.Contains("customer experience"))
            {
                return "retail companies";
            }
            
            if (inputLower.Contains("manufactur") || inputLower.Contains("factory") || inputLower.Contains("production") || 
                inputLower.Contains("industrial") || inputLower.Contains("supply chain") || inputLower.Contains("automation"))
            {
                return "manufacturing companies";
            }
            
            if (inputLower.Contains("tech") || inputLower.Contains("software") || inputLower.Contains("ai") || 
                inputLower.Contains("technology") || inputLower.Contains("digital") || inputLower.Contains("startup"))
            {
                // Tech could apply to both industries, so include both
                return "technology companies (retail and manufacturing)";
            }

            // Look for traditional audience-related patterns
            var audiencePatterns = new[]
            {
                @"(?:for |to |targeting |target )(?:our |the )?(top \d+ customers?|top customers?|enterprise customers?|small business customers?|existing customers?|new customers?|potential customers?|prospects?)",
                @"(?:for |to |targeting |target )(?:our |the )?(enterprise|small business|mid-market|startup|fortune 500|large enterprise|sme|smb) (?:customers?|clients?|companies?|organizations?|businesses?)",
                @"(?:for |to |targeting |target )(?:our |the )?(customers?|clients?|prospects?|audience|market|segment)",
                @"(top \d+ customers?|top customers?|enterprise customers?|small business customers?|existing customers?|new customers?|potential customers?|prospects?)"
            };

            foreach (var pattern in audiencePatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var audience = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(audience))
                    {
                        return audience;
                    }
                }
            }

            // Default: target both industries since we have mock data for both
            return "retail and manufacturing companies";
        }

        private List<string> ExtractComponents(string input)
        {
            var components = new List<string>();
            var inputLower = input.ToLower();

            // Define component mappings based on our actual available tools
            var componentMappings = new Dictionary<string, string[]>
            {
                ["landing page"] = new[] { "landing page", "landing site", "website", "web page", "site", "page" },
                ["email"] = new[] { "email", "emails", "email campaign", "email blast", "newsletter", "email marketing" },
                ["linkedin post"] = new[] { "linkedin", "linkedin post", "social media", "social post", "social content", "social" },
                ["ad copy"] = new[] { "ads", "ad", "advertising", "ad copy", "advertisements", "paid ads", "google ads", "facebook ads", "marketing copy" },
                ["company brief"] = new[] { "brief", "company brief", "briefing", "company profile", "analysis", "research", "insight" }
            };

            // Look for explicit component mentions
            foreach (var mapping in componentMappings)
            {
                var componentName = mapping.Key;
                var keywords = mapping.Value;

                foreach (var keyword in keywords)
                {
                    if (inputLower.Contains(keyword))
                    {
                        if (!components.Contains(componentName))
                        {
                            components.Add(componentName);
                        }
                        break;
                    }
                }
            }

            // Look for action-based patterns and map to our available tools
            var actionPatterns = new[]
            {
                @"(?:build|create|generate|make|develop|design|produce|write) (?:a |an |some )?([^.]+?)(?:\s+and|\s+for|\s+to|\s*$)",
                @"(?:that )(?:build|create|generate|make|develop|design|produce|write)s? (?:a |an |some )?([^.]+?)(?:\s+and|\s+for|\s+to|\s*$)"
            };

            foreach (var pattern in actionPatterns)
            {
                var matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var content = match.Groups[1].Value.Trim().ToLower();
                    
                    // Map content to our available components
                    foreach (var mapping in componentMappings)
                    {
                        var componentName = mapping.Key;
                        var keywords = mapping.Value;

                        if (keywords.Any(k => content.Contains(k)))
                        {
                            if (!components.Contains(componentName))
                            {
                                components.Add(componentName);
                            }
                            break;
                        }
                    }
                }
            }

            // Look for campaign-related keywords and add appropriate components
            if (inputLower.Contains("campaign") || inputLower.Contains("marketing"))
            {
                // Add default campaign components if none specified
                if (components.Count == 0)
                {
                    components.AddRange(new[] { "company brief", "landing page", "email" });
                }
                else if (!components.Contains("company brief"))
                {
                    // Always include company brief for campaigns
                    components.Insert(0, "company brief");
                }
            }

            // Multi-company campaign detection
            if (inputLower.Contains("companies") || inputLower.Contains("multiple") || 
                inputLower.Contains("different") || inputLower.Contains("various"))
            {
                if (!components.Contains("company brief"))
                {
                    components.Insert(0, "company brief");
                }
            }

            // Default components if none found - prioritize our mock data capabilities
            if (components.Count == 0)
            {
                components.AddRange(new[] { "company brief", "landing page", "email" });
            }

            return components;
        }

        private async Task<ParsedCampaign> EnhanceWithAI(string input, ParsedCampaign ruleBasedResult)
        {
            try
            {
                var enhancementPrompt = $@"
You are an expert marketing campaign analyzer. Please analyze the following natural language input and extract/enhance the campaign components.

Available Industries (with mock data): retail, manufacturing
Available Tools: company brief, landing page, email, linkedin post, ad copy

Input: ""{input}""

Current extraction:
- Goal: {ruleBasedResult.Goal}
- Audience: {ruleBasedResult.Audience}
- Components: {string.Join(", ", ruleBasedResult.Components)}

Please provide enhanced versions that are more natural and specific. Focus on:
- Goal: Make specific to retail/manufacturing industries when possible
- Audience: Should be ""retail companies"", ""manufacturing companies"", or ""retail and manufacturing companies""
- Components: Only use available tools: company brief, landing page, email, linkedin post, ad copy

Respond in this exact format:
GOAL: [enhanced goal description]
AUDIENCE: [enhanced audience description - must be retail/manufacturing focused]
COMPONENTS: [comma-separated list using only available tools]
";

                var response = await _kernel.InvokePromptAsync(enhancementPrompt);
                var responseText = response.ToString();

                // Parse the AI response
                var enhanced = new ParsedCampaign();
                
                var goalMatch = Regex.Match(responseText, @"GOAL:\s*(.+?)(?=\n|AUDIENCE:|COMPONENTS:|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (goalMatch.Success)
                {
                    enhanced.Goal = goalMatch.Groups[1].Value.Trim();
                }

                var audienceMatch = Regex.Match(responseText, @"AUDIENCE:\s*(.+?)(?=\n|GOAL:|COMPONENTS:|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (audienceMatch.Success)
                {
                    enhanced.Audience = audienceMatch.Groups[1].Value.Trim();
                }

                var componentsMatch = Regex.Match(responseText, @"COMPONENTS:\s*(.+?)(?=\n|GOAL:|AUDIENCE:|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (componentsMatch.Success)
                {
                    var componentsList = componentsMatch.Groups[1].Value.Split(',')
                        .Select(c => c.Trim())
                        .Where(c => !string.IsNullOrEmpty(c))
                        .Where(c => IsValidComponent(c)) // Validate against available tools
                        .ToList();
                    enhanced.Components = componentsList;
                }

                // Use AI enhancements if they're better, otherwise fall back to rule-based
                return new ParsedCampaign
                {
                    Goal = !string.IsNullOrEmpty(enhanced.Goal) ? enhanced.Goal : ruleBasedResult.Goal,
                    Audience = !string.IsNullOrEmpty(enhanced.Audience) ? enhanced.Audience : ruleBasedResult.Audience,
                    Components = enhanced.Components.Any() ? enhanced.Components : ruleBasedResult.Components
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AI enhancement failed: {ex.Message}. Using rule-based results.");
                return ruleBasedResult;
            }
        }

        /// <summary>
        /// Validates if a component name matches our available tools
        /// </summary>
        private bool IsValidComponent(string component)
        {
            var validComponents = new[] { "company brief", "landing page", "email", "linkedin post", "ad copy" };
            return validComponents.Any(vc => vc.Equals(component, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Represents a parsed campaign from natural language input
    /// </summary>
    public class ParsedCampaign
    {
        public string Goal { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public List<string> Components { get; set; } = new List<string>();
    }
}
