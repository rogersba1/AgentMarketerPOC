# Human-in-the-Loop Implementation for Company Brief Review

## Overview

This document describes the implementation of a human-in-the-loop workflow for reviewing and approving generated company briefs before continuing with campaign content generation. This follows Semantic Kernel best practices for stepwise planning, function calling, and context management.

## Architecture

### Key Components

1. **Company Brief Generation Function** (`ContentGenerationTools.GenerateCompanyBrief`)
   - Generates comprehensive company targeting briefs
   - Uses company research data and campaign goals
   - Creates detailed strategy documents for human review

2. **Human Approval Workflow** (`RouterAgent` approval methods)
   - Pauses campaign execution at approval points
   - Stores approval state in plan steps
   - Manages human feedback and modifications

3. **CLI Review Interface** (`Program.ReviewCompanyBriefs`)
   - Interactive command-line interface for reviewing briefs
   - Commands for approve, modify, reject, and continue
   - Real-time campaign state management

### Semantic Kernel Integration

#### Enhanced PlanStep Model
```csharp
public class PlanStep
{
    // Existing properties...
    public bool RequiresHumanApproval { get; set; } = false;
    public HumanApprovalStatus ApprovalStatus { get; set; } = HumanApprovalStatus.NotRequired;
    public string? HumanFeedback { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public enum HumanApprovalStatus
{
    NotRequired,
    PendingReview,
    Approved,
    ApprovedWithModifications,
    Rejected
}
```

#### Function Registration
The company brief generation function is registered as a Semantic Kernel function:

```csharp
[KernelFunction("generate_company_brief")]
[Description("Generates a detailed company brief for targeting strategy based on research and campaign goals")]
public async Task<string> GenerateCompanyBrief(
    [Description("Campaign goal describing the key message")] string goal,
    [Description("Target company name")] string companyName,
    [Description("Industry insights and company research data")] string insights = "")
```

## Workflow Implementation

### 1. Plan Generation with Human Approval Steps

The `PlannerAgent` now creates plan steps that require human approval:

```csharp
// Step: Generate company brief (requires human approval)
plan.Steps.Add(new PlanStep
{
    Name = $"Generate Company Brief for {company.BasicInfo.CompanyName}",
    Description = $"Create a comprehensive targeting brief for {company.BasicInfo.CompanyName}",
    AgentType = "ContentTool",
    Function = "generate_company_brief",
    RequiresHumanApproval = true,
    ApprovalStatus = HumanApprovalStatus.PendingReview
});

// Step: Review company brief (human-in-the-loop)
plan.Steps.Add(new PlanStep
{
    Name = $"Review Company Brief for {company.BasicInfo.CompanyName}",
    Description = $"Human review and approval of the generated company brief",
    AgentType = "RouterAgent",
    Function = "ReviewCompanyBrief",
    RequiresHumanApproval = true,
    ApprovalStatus = HumanApprovalStatus.PendingReview
});
```

### 2. Execution Pause and Resume

The `RouterAgent.ExecutePlan` method checks for pending approvals:

```csharp
// Check for any pending human approvals
var pendingApprovalSteps = plan.Steps.Where(s => 
    s.RequiresHumanApproval && s.ApprovalStatus == HumanApprovalStatus.PendingReview).ToList();

if (pendingApprovalSteps.Any())
{
    // Generate content for review and pause execution
    // Return instructions for human review
}
```

### 3. Human Review Interface

#### CLI Commands
- `review brief [company name]` - Display company brief for review
- `approve brief [company name]` - Approve brief as-is
- `modify brief [company name] [feedback]` - Approve with modifications
- `reject brief [company name] [reason]` - Reject the brief
- `list` - Show all pending approvals
- `continue` - Resume campaign execution

#### Example Usage
```
Company Brief Review Commands:
• list - Show pending approvals
• review TechCorp - Review the TechCorp company brief
• approve TechCorp - Approve the TechCorp brief
• modify TechCorp "Focus more on their retail expansion" - Approve with changes
• reject TechCorp "Insufficient market research" - Reject the brief
• continue - Continue campaign execution
```

## State Management

### Session Persistence
- Campaign sessions are persisted with approval states
- Human feedback is stored in plan steps
- Sessions can be resumed after approval actions

### Context Preservation
- Generated briefs are stored in `session.Campaign.GeneratedContent`
- Human feedback is preserved in `step.HumanFeedback`
- Approval timestamps are recorded in `step.ReviewedAt`

## Generated Company Brief Format

Each company brief includes:

```markdown
# Company Brief: [Company Name]

## Executive Summary
[Summary of company and campaign alignment]

## Company Overview
- Company Name, Industry, Size, Location, Website, Founded

## Financial & Performance Metrics
- Growth rate, customer satisfaction, market share, active clients

## Campaign Alignment Analysis
- Why this company fits the campaign
- Key messaging pillars

## Personalization Strategy
- Landing page approach
- Email strategy
- LinkedIn engagement
- Ad targeting

## Risk Assessment
- Market position analysis
- Competitive considerations

## Success Metrics
- Engagement targets
- Conversion goals
- Timeline expectations

## Budget Allocation Recommendation
- Content creation, advertising, tools, follow-up

## Timeline
- Week-by-week execution plan
```

## API Integration

### CampaignOrchestrationService Methods

```csharp
// Get router agent for direct approval access
public RouterAgent GetRouterAgent()

// Load campaign session
public async Task<CampaignSession> GetSessionAsync(string sessionId)

// Review company brief with commands
public async Task<string> ReviewCompanyBriefAsync(string sessionId, string command)

// Approve brief with feedback
public async Task<string> ApproveCompanyBriefAsync(string sessionId, string companyName, 
    string feedback = "", bool isModified = false, bool isRejected = false)
```

### RouterAgent Approval Methods

```csharp
// Display brief for human review
public Task<string> ReviewCompanyBriefForApproval(string companyName, CampaignSession session)

// Process approval/rejection with feedback
public Task<string> ApproveBrief(string companyName, string feedback, CampaignSession session, 
    bool isApproved = true, bool isModified = false)

// List all pending approvals
public Task<string> ListPendingApprovals(CampaignSession session)
```

## Best Practices Implemented

### Semantic Kernel Best Practices
1. **Function Composition**: Company brief generation uses the `[KernelFunction]` attribute
2. **Parameter Description**: All function parameters are properly described for the planner
3. **Stepwise Planning**: Plan execution pauses at human decision points
4. **Context Management**: Session state is preserved across human interactions
5. **Error Handling**: Comprehensive error handling for missing sessions/steps

### Human-in-the-Loop Best Practices
1. **Clear Instructions**: Users receive clear commands and usage examples
2. **State Visibility**: Current approval status is always visible
3. **Flexible Feedback**: Support for approval, modification, and rejection with custom feedback
4. **Resume Capability**: Campaigns can be paused and resumed after review
5. **Audit Trail**: All human actions are logged with timestamps

## Usage Example

1. **Start Campaign**: Create campaign targeting retail companies
2. **Generate Plan**: System creates plan with company brief generation steps
3. **Execute Until Approval**: System generates company briefs and pauses
4. **Human Review**: User reviews brief using CLI commands
5. **Approve/Modify**: User provides feedback and approves brief
6. **Continue Execution**: System resumes and generates marketing content
7. **Repeat**: Process repeats for each target company

## Benefits

1. **Quality Control**: Human oversight ensures appropriate targeting and messaging
2. **Flexibility**: Briefs can be modified before content generation
3. **Efficiency**: Only briefing requires human review, content generation is automated
4. **Compliance**: Human approval ensures marketing campaigns meet company standards
5. **Learning**: Human feedback can improve future brief generation

## Future Enhancements

1. **Web Interface**: Browser-based review interface
2. **Team Collaboration**: Multiple reviewers and approval workflows
3. **Template Customization**: Custom brief templates by industry
4. **AI Suggestions**: AI-powered suggestions for brief improvements
5. **Bulk Operations**: Approve/modify multiple briefs at once
