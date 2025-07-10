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
        private readonly MockCompanyDataService _companyDataService;

        public ResearcherAgent(Kernel kernel) : base(kernel, RESEARCHER_SYSTEM_PROMPT)
        {
            _mockCustomerData = InitializeMockCustomerData();
            _companyDataService = new MockCompanyDataService();
            
            // Initialize company data asynchronously
            _ = Task.Run(async () => await _companyDataService.LoadCompanyDataAsync());
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Processing insights request for '{input}'");

                // Check if the input is asking for company-specific insights
                if (input.ToLower().Contains("company") || input.ToLower().Contains("business") || 
                    input.ToLower().Contains("enterprise") || input.ToLower().Contains("organization"))
                {
                    var companyInsights = await GetCompanyInsights(input);
                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Researcher: Generated company insights");
                    return companyInsights;
                }

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

        /// <summary>
        /// Get insights from mock company data
        /// </summary>
        public async Task<string> GetCompanyInsights(string input)
        {
            try
            {
                await _companyDataService.LoadCompanyDataAsync();
                
                // Parse the input to understand what kind of company insights are needed
                var inputLower = input.ToLower();
                
                if (inputLower.Contains("top") && (inputLower.Contains("20") || inputLower.Contains("twenty")))
                {
                    // Get top 20 companies by revenue
                    var topCompanies = _companyDataService.GetTopCompaniesByRevenue(20);
                    return GenerateTopCompaniesInsights(topCompanies);
                }
                else if (inputLower.Contains("retail"))
                {
                    // Get retail industry analysis
                    return _companyDataService.GetIndustryAnalysis("retail");
                }
                else if (inputLower.Contains("manufacturing"))
                {
                    // Get manufacturing industry analysis
                    return _companyDataService.GetIndustryAnalysis("manufacturing");
                }
                else if (inputLower.Contains("enterprise") || inputLower.Contains("large"))
                {
                    // Get large enterprises (200+ employees)
                    var largeCompanies = _companyDataService.GetCompaniesBySize(200, 500);
                    return GenerateEnterpriseInsights(largeCompanies);
                }
                else
                {
                    // Default to general company insights
                    var allCompanies = _companyDataService.GetAllCompanies();
                    return GenerateGeneralCompanyInsights(allCompanies);
                }
            }
            catch (Exception ex)
            {
                return $"Error retrieving company insights: {ex.Message}";
            }
        }

        private string GenerateTopCompaniesInsights(List<CompanyProfile> companies)
        {
            if (!companies.Any())
                return "No company data available for analysis.";

            var totalRevenue = companies.Sum(c => ParseRevenue(c.BusinessDetails.RevenueEstimate));
            var avgEmployees = companies.Average(c => c.Leadership.Employees);
            var industryDistribution = companies.GroupBy(c => GetIndustryCategory(c.BasicInfo.Industry))
                .ToDictionary(g => g.Key, g => g.Count());

            return $@"## Top 20 Companies Analysis

**Market Overview:**
• Total Combined Revenue: ${totalRevenue:F0} million
• Average Company Size: {avgEmployees:F0} employees
• Industries Represented: {industryDistribution.Count}

**Industry Distribution:**
{string.Join("\n", industryDistribution.Select(kv => $"• {kv.Key}: {kv.Value} companies"))}

**Key Insights:**
• Revenue Range: ${companies.Min(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0}M - ${companies.Max(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0}M
• Average Growth Rate: {companies.Average(c => ParsePercentage(c.Metrics.AnnualGrowthRate)):F1}%
• Average Customer Satisfaction: {companies.Average(c => ParsePercentage(c.Metrics.CustomerSatisfactionScore)):F1}%

**Top 5 Companies by Revenue:**
{string.Join("\n", companies.Take(5).Select((c, i) => $"{i + 1}. {c.BasicInfo.CompanyName} - {c.BusinessDetails.RevenueEstimate} ({c.BasicInfo.Industry})"))}

**Marketing Recommendations:**
• Focus on industry-specific messaging for {industryDistribution.Keys.First()} sector
• Emphasize ROI and growth potential (avg {companies.Average(c => ParsePercentage(c.Metrics.AnnualGrowthRate)):F1}% growth)
• Target decision-makers in companies with 200+ employees
• Highlight technology and innovation themes
";
        }

        private string GenerateEnterpriseInsights(List<CompanyProfile> companies)
        {
            if (!companies.Any())
                return "No enterprise company data available for analysis.";

            return $@"## Enterprise Customer Analysis

**Enterprise Segment Overview:**
• Total Companies: {companies.Count}
• Average Revenue: ${companies.Average(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0} million
• Average Employees: {companies.Average(c => c.Leadership.Employees):F0}
• Combined Market Value: ${companies.Sum(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0} million

**Key Characteristics:**
• Established businesses (avg founded {companies.Average(c => c.BasicInfo.Founded):F0})
• High customer satisfaction ({companies.Average(c => ParsePercentage(c.Metrics.CustomerSatisfactionScore)):F1}%)
• Strong growth trajectory ({companies.Average(c => ParsePercentage(c.Metrics.AnnualGrowthRate)):F1}% annually)

**Common Challenges & Opportunities:**
• Digital transformation and automation needs
• Sustainability and efficiency focus
• Competitive pressure requiring innovation
• Need for scalable solutions

**Recommended Campaign Approach:**
• Emphasize enterprise-grade security and compliance
• Highlight scalability and integration capabilities
• Focus on ROI and measurable business outcomes
• Target C-level executives and IT decision-makers
• Use case studies and testimonials from similar enterprises
";
        }

        private string GenerateGeneralCompanyInsights(List<CompanyProfile> companies)
        {
            if (!companies.Any())
                return "No company data available for analysis.";

            var retailCount = companies.Count(c => c.BasicInfo.Industry.ToLower().Contains("retail"));
            var manufacturingCount = companies.Count(c => c.BasicInfo.Industry.ToLower().Contains("manufacturing"));

            return $@"## General Business Market Analysis

**Market Composition:**
• Total Companies Analyzed: {companies.Count}
• Retail Companies: {retailCount} ({(double)retailCount / companies.Count * 100:F1}%)
• Manufacturing Companies: {manufacturingCount} ({(double)manufacturingCount / companies.Count * 100:F1}%)

**Business Landscape:**
• Revenue Range: ${companies.Min(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0}M - ${companies.Max(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0}M
• Average Company Size: {companies.Average(c => c.Leadership.Employees):F0} employees
• Average Growth Rate: {companies.Average(c => ParsePercentage(c.Metrics.AnnualGrowthRate)):F1}%
• Average Customer Satisfaction: {companies.Average(c => ParsePercentage(c.Metrics.CustomerSatisfactionScore)):F1}%

**Market Trends:**
• Strong focus on digital transformation
• Emphasis on sustainability and efficiency
• Growing importance of customer experience
• Increasing adoption of AI and automation

**Campaign Opportunities:**
• Technology solutions for operational efficiency
• Sustainability and environmental responsibility
• Customer experience enhancement
• Data-driven decision making tools
• Innovation and competitive advantage
";
        }

        private string GetIndustryCategory(string fullIndustry)
        {
            if (fullIndustry.ToLower().Contains("retail"))
                return "Retail";
            else if (fullIndustry.ToLower().Contains("manufacturing"))
                return "Manufacturing";
            else
                return "Other";
        }

        private double ParseRevenue(string revenueString)
        {
            var cleanString = revenueString.Replace("$", "").Replace(" million", "").Replace(" annually", "").Replace(",", "");
            if (double.TryParse(cleanString, out double revenue))
                return revenue;
            return 0;
        }

        private double ParsePercentage(string percentageString)
        {
            var cleanString = percentageString.Replace("%", "");
            if (double.TryParse(cleanString, out double percentage))
                return percentage;
            return 0;
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

            if (audienceLower.Contains("enterprise") || audienceLower.Contains("large"))
            {
                return _mockCustomerData.Where(c => c.Segment == "Enterprise").ToList();
            }
            else if (audienceLower.Contains("small") || audienceLower.Contains("startup"))
            {
                return _mockCustomerData.Where(c => c.Segment == "Small Business").ToList();
            }
            else if (audienceLower.Contains("mid") || audienceLower.Contains("medium"))
            {
                return _mockCustomerData.Where(c => c.Segment == "Mid-Market").ToList();
            }
            else if (audienceLower.Contains("top") && (audienceLower.Contains("20") || audienceLower.Contains("customer")))
            {
                return _mockCustomerData.OrderByDescending(c => c.Revenue).Take(20).ToList();
            }
            else if (audienceLower.Contains("recent") || audienceLower.Contains("active"))
            {
                return _mockCustomerData.OrderByDescending(c => c.LastEngagement).Take(10).ToList();
            }
            else
            {
                // Default to all customers
                return _mockCustomerData.ToList();
            }
        }

        private Dictionary<string, object> AnalyzeCustomers(List<Customer> customers)
        {
            var insights = new Dictionary<string, object>();

            if (customers.Any())
            {
                insights["Total Customers"] = customers.Count.ToString();
                insights["Average Revenue"] = $"${customers.Average(c => c.Revenue):F0}";
                insights["Primary Segment"] = customers.GroupBy(c => c.Segment)
                    .OrderByDescending(g => g.Count())
                    .First().Key;
                insights["Recent Engagement"] = $"{customers.Count(c => c.LastEngagement > DateTime.UtcNow.AddDays(-7))} customers active in last 7 days";
                insights["High Value Customers"] = $"{customers.Count(c => c.Revenue > 100000)} customers with >$100K revenue";
            }

            return insights;
        }

        private List<string> GenerateRecommendations(List<Customer> customers, string audience)
        {
            var recommendations = new List<string>();

            if (customers.Any())
            {
                var topInterests = GetTopInterests(customers);
                var avgRevenue = customers.Average(c => c.Revenue);
                var primarySegment = customers.GroupBy(c => c.Segment)
                    .OrderByDescending(g => g.Count())
                    .First().Key;

                recommendations.Add($"Focus messaging on {string.Join(", ", topInterests.Take(3))} themes");
                recommendations.Add($"Target {primarySegment} segment with tailored content");
                
                if (avgRevenue > 100000)
                {
                    recommendations.Add("Emphasize enterprise-grade features and ROI");
                }
                else if (avgRevenue < 50000)
                {
                    recommendations.Add("Highlight cost-effectiveness and ease of use");
                }
                else
                {
                    recommendations.Add("Balance feature richness with value proposition");
                }

                var recentEngagement = customers.Count(c => c.LastEngagement > DateTime.UtcNow.AddDays(-30));
                if (recentEngagement > customers.Count * 0.7)
                {
                    recommendations.Add("Leverage high engagement with personalized follow-ups");
                }
                else
                {
                    recommendations.Add("Focus on re-engagement campaigns for dormant customers");
                }
            }

            return recommendations;
        }

        private List<string> GetTopInterests(List<Customer> customers)
        {
            return customers
                .SelectMany(c => c.Interests)
                .GroupBy(interest => interest)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(5)
                .ToList();
        }
    }
}
