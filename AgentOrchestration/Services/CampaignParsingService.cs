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

            // Fallback: look for key topics/subjects
            var topicPatterns = new[]
            {
                @"(manufacturing automation|digital transformation|AI capabilities|marketing automation|cloud solutions|data analytics|cybersecurity|software development|product launch|brand awareness)",
                @"(automation|AI|marketing|sales|productivity|efficiency|growth|innovation|technology|digital)"
            };

            foreach (var pattern in topicPatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return $"Promote {match.Groups[1].Value} benefits and capabilities";
                }
            }

            return "Drive business growth and engagement";
        }

        private string ExtractAudience(string input)
        {
            // Look for audience-related patterns
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

            return "Target customers";
        }

        private List<string> ExtractComponents(string input)
        {
            var components = new List<string>();
            var inputLower = input.ToLower();

            // Define component mappings
            var componentMappings = new Dictionary<string, string[]>
            {
                ["landing page"] = new[] { "landing page", "landing site", "website", "web page", "site" },
                ["email"] = new[] { "email", "emails", "email campaign", "email blast", "newsletter" },
                ["linkedin post"] = new[] { "linkedin", "linkedin post", "social media", "social post", "social content" },
                ["ads"] = new[] { "ads", "ad", "advertising", "ad copy", "advertisements", "paid ads", "google ads", "facebook ads" },
                ["images"] = new[] { "images", "graphics", "visuals", "artwork", "creative", "designs" },
                ["video"] = new[] { "video", "videos", "video content", "promotional video" },
                ["blog post"] = new[] { "blog", "blog post", "article", "content", "written content" },
                ["brochure"] = new[] { "brochure", "brochures", "flyer", "pamphlet", "print material" },
                ["presentation"] = new[] { "presentation", "slides", "deck", "powerpoint", "pitch deck" }
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

            // Look for action-based patterns
            var actionPatterns = new[]
            {
                @"(?:build|create|generate|make|develop|design|produce) (?:a |an |some )?([^.]+?)(?:\s+and|\s+for|\s+to|\s*$)",
                @"(?:that )(?:build|create|generate|make|develop|design|produce)s? (?:a |an |some )?([^.]+?)(?:\s+and|\s+for|\s+to|\s*$)"
            };

            foreach (var pattern in actionPatterns)
            {
                var matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    var content = match.Groups[1].Value.Trim().ToLower();
                    
                    // Map content to components
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

            // Default components if none found
            if (components.Count == 0)
            {
                components.AddRange(new[] { "landing page", "email" });
            }

            return components;
        }

        private async Task<ParsedCampaign> EnhanceWithAI(string input, ParsedCampaign ruleBasedResult)
        {
            try
            {
                var enhancementPrompt = $@"
You are an expert marketing campaign analyzer. Please analyze the following natural language input and extract/enhance the campaign components:

Input: ""{input}""

Current extraction:
- Goal: {ruleBasedResult.Goal}
- Audience: {ruleBasedResult.Audience}
- Components: {string.Join(", ", ruleBasedResult.Components)}

Please provide enhanced versions that are more natural and specific. Respond in this exact format:
GOAL: [enhanced goal description]
AUDIENCE: [enhanced audience description]
COMPONENTS: [comma-separated list of components]

Focus on making the goal more specific and actionable, the audience more precise, and ensuring components match the user's intent.
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
