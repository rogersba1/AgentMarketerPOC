using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AgentOrchestration.Models;

namespace AgentOrchestration.Services
{
    /// <summary>
    /// Service for loading and managing mock company data
    /// </summary>
    public class MockCompanyDataService
    {
        private List<CompanyProfile>? _retailCompanies;
        private List<CompanyProfile>? _manufacturingCompanies;
        private CompanyDataIndex? _companyIndex;
        private readonly string _dataPath;

        public MockCompanyDataService()
        {
            // Get the path to the Data directory
            var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
            _dataPath = Path.Combine(assemblyDirectory ?? "", "Data", "Companies");
        }

        /// <summary>
        /// Load all company data from JSON files
        /// </summary>
        public async Task LoadCompanyDataAsync()
        {
            try
            {
                // Load retail companies
                var retailPath = Path.Combine(_dataPath, "retail_companies.json");
                if (File.Exists(retailPath))
                {
                    var retailJson = await File.ReadAllTextAsync(retailPath);
                    _retailCompanies = JsonSerializer.Deserialize<List<CompanyProfile>>(retailJson);
                }

                // Load manufacturing companies
                var manufacturingPath = Path.Combine(_dataPath, "manufacturing_companies.json");
                if (File.Exists(manufacturingPath))
                {
                    var manufacturingJson = await File.ReadAllTextAsync(manufacturingPath);
                    _manufacturingCompanies = JsonSerializer.Deserialize<List<CompanyProfile>>(manufacturingJson);
                }

                // Load index
                var indexPath = Path.Combine(_dataPath, "companies_index.json");
                if (File.Exists(indexPath))
                {
                    var indexJson = await File.ReadAllTextAsync(indexPath);
                    _companyIndex = JsonSerializer.Deserialize<CompanyDataIndex>(indexJson);
                }
            }
            catch (Exception ex)
            {
                // Fall back to hardcoded data if files don't exist
                await InitializeHardcodedDataAsync();
            }
        }

        /// <summary>
        /// Get all companies from both industries
        /// </summary>
        public List<CompanyProfile> GetAllCompanies()
        {
            var allCompanies = new List<CompanyProfile>();
            
            if (_retailCompanies != null)
                allCompanies.AddRange(_retailCompanies);
            
            if (_manufacturingCompanies != null)
                allCompanies.AddRange(_manufacturingCompanies);
            
            return allCompanies;
        }

        /// <summary>
        /// Get companies by industry
        /// </summary>
        public List<CompanyProfile> GetCompaniesByIndustry(string industry)
        {
            var allCompanies = GetAllCompanies();
            return allCompanies
                .Where(c => c.BasicInfo.Industry.ToLower().Contains(industry.ToLower()))
                .ToList();
        }

        /// <summary>
        /// Get a specific company by ID
        /// </summary>
        public CompanyProfile? GetCompanyById(string companyId)
        {
            var allCompanies = GetAllCompanies();
            return allCompanies.FirstOrDefault(c => c.CompanyId == companyId);
        }

        /// <summary>
        /// Get a company by name
        /// </summary>
        public CompanyProfile? GetCompanyByName(string companyName)
        {
            var allCompanies = GetAllCompanies();
            return allCompanies.FirstOrDefault(c => 
                c.BasicInfo.CompanyName.Equals(companyName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get top N companies by revenue
        /// </summary>
        public List<CompanyProfile> GetTopCompaniesByRevenue(int count = 10)
        {
            var allCompanies = GetAllCompanies();
            return allCompanies
                .OrderByDescending(c => ParseRevenue(c.BusinessDetails.RevenueEstimate))
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get companies by size (employee count)
        /// </summary>
        public List<CompanyProfile> GetCompaniesBySize(int minEmployees, int maxEmployees)
        {
            var allCompanies = GetAllCompanies();
            return allCompanies
                .Where(c => c.Leadership.Employees >= minEmployees && c.Leadership.Employees <= maxEmployees)
                .ToList();
        }

        /// <summary>
        /// Get companies founded in a specific year range
        /// </summary>
        public List<CompanyProfile> GetCompaniesByFoundedYear(int startYear, int endYear)
        {
            var allCompanies = GetAllCompanies();
            return allCompanies
                .Where(c => c.BasicInfo.Founded >= startYear && c.BasicInfo.Founded <= endYear)
                .ToList();
        }

        /// <summary>
        /// Search companies by name or description
        /// </summary>
        public List<CompanyProfile> SearchCompanies(string searchTerm)
        {
            var allCompanies = GetAllCompanies();
            var lowerSearchTerm = searchTerm.ToLower();
            
            return allCompanies
                .Where(c => 
                    c.BasicInfo.CompanyName.ToLower().Contains(lowerSearchTerm) ||
                    c.BusinessDetails.MissionStatement.ToLower().Contains(lowerSearchTerm) ||
                    c.BusinessDetails.ProductsServices.Any(p => p.ToLower().Contains(lowerSearchTerm)) ||
                    c.BusinessDetails.TargetMarket.ToLower().Contains(lowerSearchTerm))
                .ToList();
        }

        /// <summary>
        /// Get industry analysis for marketing insights
        /// </summary>
        public string GetIndustryAnalysis(string industry)
        {
            var companies = GetCompaniesByIndustry(industry);
            if (!companies.Any())
                return $"No companies found in {industry} industry.";

            var avgRevenue = companies.Average(c => ParseRevenue(c.BusinessDetails.RevenueEstimate));
            var avgEmployees = companies.Average(c => c.Leadership.Employees);
            var avgGrowthRate = companies.Average(c => ParsePercentage(c.Metrics.AnnualGrowthRate));
            var avgSatisfaction = companies.Average(c => ParsePercentage(c.Metrics.CustomerSatisfactionScore));

            return $@"
## {industry} Industry Analysis

**Market Overview**:
• Total Companies Analyzed: {companies.Count}
• Average Revenue: ${avgRevenue:F0} million
• Average Company Size: {avgEmployees:F0} employees
• Average Growth Rate: {avgGrowthRate:F1}%
• Average Customer Satisfaction: {avgSatisfaction:F1}%

**Key Trends**:
• Companies range from ${companies.Min(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0}M to ${companies.Max(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)):F0}M in revenue
• Employee count ranges from {companies.Min(c => c.Leadership.Employees)} to {companies.Max(c => c.Leadership.Employees)}
• Growth rates vary from {companies.Min(c => ParsePercentage(c.Metrics.AnnualGrowthRate)):F1}% to {companies.Max(c => ParsePercentage(c.Metrics.AnnualGrowthRate)):F1}%

**Common Services/Products**:
{string.Join("\n", companies.SelectMany(c => c.BusinessDetails.ProductsServices).GroupBy(p => p).OrderByDescending(g => g.Count()).Take(5).Select(g => $"• {g.Key} ({g.Count()} companies)"))}

**Top Performing Companies**:
{string.Join("\n", companies.OrderByDescending(c => ParseRevenue(c.BusinessDetails.RevenueEstimate)).Take(3).Select(c => $"• {c.BasicInfo.CompanyName} - {c.BusinessDetails.RevenueEstimate}"))}
";
        }

        private async Task InitializeHardcodedDataAsync()
        {
            // Fallback to minimal hardcoded data if files aren't available
            _retailCompanies = new List<CompanyProfile>();
            _manufacturingCompanies = new List<CompanyProfile>();
            _companyIndex = new CompanyDataIndex();
        }

        private double ParseRevenue(string revenueString)
        {
            // Extract numeric value from revenue string like "$42 million annually"
            var cleanString = revenueString.Replace("$", "").Replace(" million", "").Replace(" annually", "").Replace(",", "");
            if (double.TryParse(cleanString, out double revenue))
                return revenue;
            return 0;
        }

        private double ParsePercentage(string percentageString)
        {
            // Extract numeric value from percentage string like "15%"
            var cleanString = percentageString.Replace("%", "");
            if (double.TryParse(cleanString, out double percentage))
                return percentage;
            return 0;
        }
    }
}
