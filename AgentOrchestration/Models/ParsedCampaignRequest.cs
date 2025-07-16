using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgentOrchestration.Models
{
    /// <summary>
    /// Represents the parsed user request with structured campaign parameters
    /// </summary>
    public class ParsedCampaignRequest
    {
        /// <summary>
        /// Target audience industry (retail, manufacturing, etc.)
        /// Defaults to 'retail' if not specified
        /// </summary>
        [Required]
        public string Audience { get; set; } = "retail";

        /// <summary>
        /// Number of companies to target (defaults to 3)
        /// </summary>
        [Range(1, 20)]
        public int CompanyCount { get; set; } = 3;

        /// <summary>
        /// Requested marketing components/content types
        /// Available: landing_page, email, linkedin_post, ad_copy
        /// </summary>
        [Required]
        public List<string> Components { get; set; } = new List<string>();

        /// <summary>
        /// Additional context or specific requirements from the user
        /// </summary>
        public string AdditionalContext { get; set; } = "";

        /// <summary>
        /// Confidence score of the parsing (0.0 to 1.0)
        /// </summary>
        [Range(0.0, 1.0)]
        public double ConfidenceScore { get; set; } = 1.0;

        /// <summary>
        /// Original user request for reference
        /// </summary>
        public string OriginalRequest { get; set; } = "";
    }

    /// <summary>
    /// Available content components that can be generated
    /// Maps to functions in ContentGenerationTools.cs
    /// </summary>
    public static class AvailableComponents
    {
        public const string LandingPage = "landing_page";
        public const string Email = "email";
        public const string LinkedInPost = "linkedin_post";
        public const string AdCopy = "ad_copy";

        public static readonly List<string> All = new List<string>
        {
            LandingPage,
            Email,
            LinkedInPost,
            AdCopy
        };

        public static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            { LandingPage, "Personalized HTML landing pages" },
            { Email, "Personalized email campaigns" },
            { LinkedInPost, "LinkedIn social media posts" },
            { AdCopy, "Advertisement copy and content" }
        };
    }

    /// <summary>
    /// Supported target audiences/industries
    /// </summary>
    public static class TargetAudiences
    {
        public const string Retail = "retail";
        public const string Manufacturing = "manufacturing";
        public const string Technology = "technology";
        public const string Healthcare = "healthcare";
        public const string Finance = "finance";

        public static readonly List<string> All = new List<string>
        {
            Retail,
            Manufacturing,
            Technology,
            Healthcare,
            Finance
        };
    }
}
