using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace AgentOrchestration.Services
{
    /// <summary>
    /// Service for parsing user requests using LLM to extract structured campaign parameters
    /// </summary>
    public class CampaignRequestParsingService
    {
        private readonly Kernel _kernel;

        public CampaignRequestParsingService(Kernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>
        /// Parse a user request using LLM to extract structured campaign parameters
        /// </summary>
        /// <param name="userRequest">Raw user request text</param>
        /// <returns>Parsed campaign request with structured parameters</returns>
        public async Task<ParsedCampaignRequest> ParseRequestAsync(string userRequest)
        {
            try
            {
                var parsingPrompt = CreateParsingPrompt(userRequest);
                
                var response = await _kernel.InvokePromptAsync(parsingPrompt);
                var responseText = response.GetValue<string>() ?? "";

                // Parse the JSON response
                var parsedRequest = ParseLLMResponse(responseText, userRequest);
                
                // Validate and apply defaults
                ValidateAndApplyDefaults(parsedRequest);

                return parsedRequest;
            }
            catch (Exception ex)
            {
                // Fallback to defaults if parsing fails
                return new ParsedCampaignRequest
                {
                    Audience = TargetAudiences.Retail,
                    CompanyCount = 3,
                    Components = new List<string> { AvailableComponents.LandingPage, AvailableComponents.Email },
                    AdditionalContext = $"Parsing failed: {ex.Message}",
                    ConfidenceScore = 0.1,
                    OriginalRequest = userRequest
                };
            }
        }

        private string CreateParsingPrompt(string userRequest)
        {
            return $@"
You are an expert at parsing marketing campaign requests. Analyze the user request and extract structured parameters.

**Available Target Audiences:**
- retail (default)
- manufacturing  
- technology
- healthcare
- finance

**Available Components:**
- landing_page: Personalized HTML landing pages
- email: Personalized email campaigns  
- linkedin_post: LinkedIn social media posts
- ad_copy: Advertisement copy and content

**Instructions:**
1. Identify the target audience/industry (default: retail)
2. Determine number of companies requested (default: 3, max: 20)
3. Identify which marketing components are requested
4. Extract any additional context or requirements
5. Assign a confidence score (0.0-1.0) based on how clear the request is

**User Request:** {userRequest}

**Response Format (JSON only, no other text):**
{{
    ""audience"": ""retail"",
    ""companyCount"": 3,
    ""components"": [""landing_page"", ""email""],
    ""additionalContext"": ""Any specific requirements or context"",
    ""confidenceScore"": 0.95,
    ""originalRequest"": ""{userRequest}""
}}

Respond with ONLY the JSON object, no additional text or formatting.";
        }

        private ParsedCampaignRequest ParseLLMResponse(string response, string originalRequest)
        {
            try
            {
                // Clean the response to extract JSON
                var cleanedResponse = ExtractJsonFromResponse(response);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var parsed = JsonSerializer.Deserialize<ParsedCampaignRequest>(cleanedResponse, options);
                
                if (parsed != null)
                {
                    parsed.OriginalRequest = originalRequest;
                    return parsed;
                }
            }
            catch (JsonException)
            {
                // JSON parsing failed, try to extract individual components
                return ExtractComponentsManually(response, originalRequest);
            }

            // Ultimate fallback
            return CreateDefaultRequest(originalRequest);
        }

        private string ExtractJsonFromResponse(string response)
        {
            // Remove any markdown formatting or extra text
            var cleaned = response.Trim();
            
            // Find JSON object boundaries
            var startIndex = cleaned.IndexOf('{');
            var endIndex = cleaned.LastIndexOf('}');
            
            if (startIndex >= 0 && endIndex > startIndex)
            {
                return cleaned.Substring(startIndex, endIndex - startIndex + 1);
            }
            
            return cleaned;
        }

        private ParsedCampaignRequest ExtractComponentsManually(string response, string originalRequest)
        {
            var parsed = CreateDefaultRequest(originalRequest);
            
            // Manual extraction logic as fallback
            var lowerResponse = response.ToLower();
            var lowerRequest = originalRequest.ToLower();
            
            // Extract audience
            if (lowerResponse.Contains("manufacturing") || lowerRequest.Contains("manufacturing"))
                parsed.Audience = TargetAudiences.Manufacturing;
            else if (lowerResponse.Contains("technology") || lowerRequest.Contains("tech"))
                parsed.Audience = TargetAudiences.Technology;
            else if (lowerResponse.Contains("healthcare") || lowerRequest.Contains("health"))
                parsed.Audience = TargetAudiences.Healthcare;
            else if (lowerResponse.Contains("finance") || lowerRequest.Contains("financial"))
                parsed.Audience = TargetAudiences.Finance;
            
            // Extract company count
            var numbers = System.Text.RegularExpressions.Regex.Matches(lowerRequest, @"\b(\d+)\b")
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => int.Parse(m.Value))
                .Where(n => n >= 1 && n <= 20)
                .ToList();
            
            if (numbers.Any())
                parsed.CompanyCount = numbers.First();
            
            // Extract components
            parsed.Components.Clear();
            if (lowerRequest.Contains("landing") || lowerRequest.Contains("page"))
                parsed.Components.Add(AvailableComponents.LandingPage);
            if (lowerRequest.Contains("email"))
                parsed.Components.Add(AvailableComponents.Email);
            if (lowerRequest.Contains("linkedin") || lowerRequest.Contains("social"))
                parsed.Components.Add(AvailableComponents.LinkedInPost);
            if (lowerRequest.Contains("ad") || lowerRequest.Contains("advertisement"))
                parsed.Components.Add(AvailableComponents.AdCopy);
            
            // Default to landing page + email if nothing specified
            if (!parsed.Components.Any())
            {
                parsed.Components.Add(AvailableComponents.LandingPage);
                parsed.Components.Add(AvailableComponents.Email);
            }
            
            parsed.ConfidenceScore = 0.6; // Medium confidence for manual extraction
            return parsed;
        }

        private ParsedCampaignRequest CreateDefaultRequest(string originalRequest)
        {
            return new ParsedCampaignRequest
            {
                Audience = TargetAudiences.Retail,
                CompanyCount = 3,
                Components = new List<string> { AvailableComponents.LandingPage, AvailableComponents.Email },
                AdditionalContext = "Using default parameters",
                ConfidenceScore = 0.5,
                OriginalRequest = originalRequest
            };
        }

        private void ValidateAndApplyDefaults(ParsedCampaignRequest request)
        {
            // Validate audience
            if (!TargetAudiences.All.Contains(request.Audience))
                request.Audience = TargetAudiences.Retail;
            
            // Validate company count
            if (request.CompanyCount < 1 || request.CompanyCount > 20)
                request.CompanyCount = 3;
            
            // Validate components
            request.Components = request.Components
                .Where(c => AvailableComponents.All.Contains(c))
                .Distinct()
                .ToList();
            
            if (!request.Components.Any())
            {
                request.Components.Add(AvailableComponents.LandingPage);
                request.Components.Add(AvailableComponents.Email);
            }
            
            // Validate confidence score
            if (request.ConfidenceScore < 0.0 || request.ConfidenceScore > 1.0)
                request.ConfidenceScore = 0.5;
        }
    }
}
