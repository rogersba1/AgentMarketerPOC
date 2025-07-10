using System.ComponentModel.DataAnnotations;

namespace AgentMarketer.Shared.Models;

/// <summary>
/// Represents a target company for campaigns
/// </summary>
public class Company
{
    /// <summary>
    /// Unique identifier for the company
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Company name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Industry sector
    /// </summary>
    [StringLength(100)]
    public string Industry { get; set; } = string.Empty;

    /// <summary>
    /// Year the company was founded
    /// </summary>
    [Range(1800, 2030)]
    public int? Founded { get; set; }

    /// <summary>
    /// Company headquarters location
    /// </summary>
    [StringLength(200)]
    public string Headquarters { get; set; } = string.Empty;

    /// <summary>
    /// Company website URL
    /// </summary>
    [Url]
    [StringLength(500)]
    public string Website { get; set; } = string.Empty;

    /// <summary>
    /// Company description or overview
    /// </summary>
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Estimated employee count
    /// </summary>
    [Range(1, 1000000)]
    public int? EmployeeCount { get; set; }

    /// <summary>
    /// Annual revenue (if known)
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? Revenue { get; set; }

    /// <summary>
    /// Company growth rate percentage
    /// </summary>
    [Range(-100, 1000)]
    public double? GrowthRate { get; set; }

    /// <summary>
    /// Customer satisfaction score (1-10)
    /// </summary>
    [Range(1, 10)]
    public double? CustomerSatisfaction { get; set; }

    /// <summary>
    /// Market share percentage
    /// </summary>
    [Range(0, 100)]
    public double? MarketShare { get; set; }

    /// <summary>
    /// Additional company metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}
