using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents
{
    /// <summary>
    /// Researcher agent that provides customer insights and audience data
    /// </summary>
    public class ResearcherAgent : BaseAgent
    {
        public override string Name => "Researcher";
        public override string Description => "Provides customer insights and audience analysis";

        private const string RESEARCHER_SYSTEM_PROMPT = @"
You are a Customer Research AI. Your role is to analyze customer data and provide actionable insights for marketing campaigns.

When provided with customer data, you should:
1. Analyze customer segments and behaviors
2. Identify key trends and patterns
3. Provide recommendations for campaign targeting
4. Suggest messaging strategies based on customer preferences

Focus on delivering concise, actionable insights that can inform campaign creation.
";

        private readonly List<Customer> _mockCustomerData;

        public ResearcherAgent(Kernel kernel) : base(kernel, RESEARCHER_SYSTEM_PROMPT)
        {
            _mockCustomerData = InitializeMockCustomerData();
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Processing insights request for '{input}'");

                var insights = await GetCustomerInsights(input);
                session.Insights = insights;

                var summary = $@"Customer Insights for '{input}':

Found {insights.Customers.Count} customers in this segment.

Key Insights:
{string.Join("\n", insights.Insights.Select(kv => $"- {kv.Key}: {kv.Value}"))}

Recommendations:
{string.Join("\n", insights.Recommendations.Select(r => $"- {r}"))}

Average Revenue: ${insights.Customers.Average(c => c.Revenue):F2}
Most Common Interests: {string.Join(", ", GetTopInterests(insights.Customers))}
";

                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Generated insights for {insights.Customers.Count} customers");

                return summary;
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Error - {ex.Message}");
                return $"Error gathering customer insights: {ex.Message}";
            }
        }

        public async Task<CustomerInsights> GetCustomerInsights(string audience)
        {
            // Simulate async operation
            await Task.Delay(100);

            var customers = GetCustomersForAudience(audience);
            
            var insights = new CustomerInsights
            {
                Audience = audience,
                Customers = customers,
                GeneratedAt = DateTime.UtcNow
            };

            // Generate insights based on customer data
            insights.Insights = AnalyzeCustomers(customers);
            insights.Recommendations = GenerateRecommendations(customers, audience);

            return insights;
        }

        private List<Customer> GetCustomersForAudience(string audience)
        {
            var audienceLower = audience.ToLower();
            
            // Mock logic to filter customers based on audience description
            if (audienceLower.Contains("top") && audienceLower.Contains("customer"))
            {
                return _mockCustomerData.OrderByDescending(c => c.Revenue).Take(20).ToList();
            }
            else if (audienceLower.Contains("enterprise"))
            {
                return _mockCustomerData.Where(c => c.Segment == "Enterprise").ToList();
            }
            else if (audienceLower.Contains("small business"))
            {
                return _mockCustomerData.Where(c => c.Segment == "Small Business").ToList();
            }
            else if (audienceLower.Contains("new") || audienceLower.Contains("recent"))
            {
                return _mockCustomerData.Where(c => c.LastEngagement > DateTime.UtcNow.AddDays(-30)).ToList();
            }
            else
            {
                // Return a random sample if no specific criteria match
                return _mockCustomerData.OrderBy(c => Guid.NewGuid()).Take(15).ToList();
            }
        }

        private Dictionary<string, object> AnalyzeCustomers(List<Customer> customers)
        {
            if (!customers.Any()) return new Dictionary<string, object>();

            return new Dictionary<string, object>
            {
                { "Total Customers", customers.Count },
                { "Average Revenue", $"${customers.Average(c => c.Revenue):F2}" },
                { "Top Segment", customers.GroupBy(c => c.Segment).OrderByDescending(g => g.Count()).First().Key },
                { "Engagement Rate", $"{customers.Count(c => c.LastEngagement > DateTime.UtcNow.AddDays(-30)) * 100.0 / customers.Count:F1}%" },
                { "Geographic Distribution", "North America: 60%, Europe: 25%, Asia-Pacific: 15%" }
            };
        }

        private List<string> GenerateRecommendations(List<Customer> customers, string audience)
        {
            var recommendations = new List<string>();

            if (customers.Average(c => c.Revenue) > 50000)
            {
                recommendations.Add("Focus on premium messaging highlighting ROI and business value");
                recommendations.Add("Use professional tone with technical details");
            }
            else
            {
                recommendations.Add("Emphasize cost-effectiveness and ease of use");
                recommendations.Add("Use friendly, approachable messaging");
            }

            var topInterests = GetTopInterests(customers);
            if (topInterests.Contains("AI"))
            {
                recommendations.Add("Highlight AI-powered features and automation benefits");
            }
            if (topInterests.Contains("Technology"))
            {
                recommendations.Add("Include technical specifications and integration capabilities");
            }
            if (topInterests.Contains("Marketing"))
            {
                recommendations.Add("Focus on marketing efficiency and campaign optimization");
            }

            recommendations.Add($"Best contact time: {GetBestContactTime(customers)}");
            recommendations.Add($"Preferred communication channel: {GetPreferredChannel(customers)}");

            return recommendations;
        }

        private List<string> GetTopInterests(List<Customer> customers)
        {
            return customers
                .SelectMany(c => c.Interests)
                .GroupBy(i => i)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();
        }

        private string GetBestContactTime(List<Customer> customers)
        {
            // Mock logic for best contact time
            return "Tuesday-Thursday, 10 AM - 2 PM EST";
        }

        private string GetPreferredChannel(List<Customer> customers)
        {
            // Mock logic for preferred communication channel
            return customers.Average(c => c.Revenue) > 25000 ? "Email" : "LinkedIn";
        }

        private List<Customer> InitializeMockCustomerData()
        {
            return new List<Customer>
            {
                new Customer
                {
                    Id = "1",
                    Name = "Acme Corp",
                    Email = "contact@acmecorp.com",
                    Segment = "Enterprise",
                    Revenue = 125000,
                    Interests = new List<string> { "AI", "Technology", "Marketing", "Automation" },
                    LastEngagement = DateTime.UtcNow.AddDays(-5)
                },
                new Customer
                {
                    Id = "2",
                    Name = "TechStart Inc",
                    Email = "hello@techstart.com",
                    Segment = "Small Business",
                    Revenue = 35000,
                    Interests = new List<string> { "Technology", "Growth", "Innovation" },
                    LastEngagement = DateTime.UtcNow.AddDays(-12)
                },
                new Customer
                {
                    Id = "3",
                    Name = "Global Solutions Ltd",
                    Email = "info@globalsolutions.com",
                    Segment = "Enterprise",
                    Revenue = 95000,
                    Interests = new List<string> { "Marketing", "Scale", "Efficiency" },
                    LastEngagement = DateTime.UtcNow.AddDays(-3)
                },
                new Customer
                {
                    Id = "4",
                    Name = "Digital Dynamics",
                    Email = "team@digitaldynamics.com",
                    Segment = "Mid-Market",
                    Revenue = 67000,
                    Interests = new List<string> { "AI", "Digital Transformation", "Analytics" },
                    LastEngagement = DateTime.UtcNow.AddDays(-8)
                },
                new Customer
                {
                    Id = "5",
                    Name = "Innovation Hub",
                    Email = "contact@innovationhub.com",
                    Segment = "Enterprise",
                    Revenue = 150000,
                    Interests = new List<string> { "AI", "Innovation", "Technology", "ROI" },
                    LastEngagement = DateTime.UtcNow.AddDays(-2)
                },
                new Customer
                {
                    Id = "6",
                    Name = "CloudFirst Solutions",
                    Email = "sales@cloudfirst.com",
                    Segment = "Mid-Market",
                    Revenue = 45000,
                    Interests = new List<string> { "Cloud", "Technology", "Marketing" },
                    LastEngagement = DateTime.UtcNow.AddDays(-15)
                },
                new Customer
                {
                    Id = "7",
                    Name = "NextGen Marketing",
                    Email = "info@nextgenmarketing.com",
                    Segment = "Small Business",
                    Revenue = 28000,
                    Interests = new List<string> { "Marketing", "Growth", "AI" },
                    LastEngagement = DateTime.UtcNow.AddDays(-6)
                },
                new Customer
                {
                    Id = "8",
                    Name = "Enterprise Systems Co",
                    Email = "contact@enterprisesystems.com",
                    Segment = "Enterprise",
                    Revenue = 200000,
                    Interests = new List<string> { "Enterprise", "Systems", "Integration", "ROI" },
                    LastEngagement = DateTime.UtcNow.AddDays(-4)
                },
                new Customer
                {
                    Id = "9",
                    Name = "Smart Analytics Inc",
                    Email = "hello@smartanalytics.com",
                    Segment = "Mid-Market",
                    Revenue = 75000,
                    Interests = new List<string> { "Analytics", "AI", "Data", "Insights" },
                    LastEngagement = DateTime.UtcNow.AddDays(-10)
                },
                new Customer
                {
                    Id = "10",
                    Name = "Future Tech Ventures",
                    Email = "team@futuretech.com",
                    Segment = "Enterprise",
                    Revenue = 180000,
                    Interests = new List<string> { "Technology", "Innovation", "AI", "Future" },
                    LastEngagement = DateTime.UtcNow.AddDays(-1)
                }
            };
        }
    }
}
