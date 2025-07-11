namespace AgentMarketer.Web.Models
{
    public record CompanyBriefResponse
    {
        public string CompanyId { get; init; } = "";
        public string CompanyName { get; init; } = "";
        public string Content { get; init; } = "";
        public string Brief { get; init; } = "";
        public string Industry { get; init; } = "";
        public string CampaignId { get; init; } = "";
        public ApprovalStatus Status { get; init; }
        public DateTime GeneratedAt { get; init; }
        public List<string>? KeyMessages { get; init; }
        public string? TargetAudience { get; init; }
        public int EstimatedBudget { get; init; }
        public int ProjectedReach { get; init; }
        public string? ApproverFeedback { get; init; }
        public DateTime? ApprovedAt { get; init; }
        public string? ApprovedBy { get; init; }
    }

    public record ApprovalRequest
    {
        public ApprovalStatus Action { get; set; }
        public string Feedback { get; set; } = "";
        public bool IsApproved { get; set; }
        public string ApprovedBy { get; set; } = "";
        public string? ModifiedContent { get; set; }
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
}
