# Human-in-the-Loop Demo Script

This demo script shows how to test the human-in-the-loop company brief review functionality.

## Setup

1. **Start the Application**
   ```bash
   dotnet run --project AgentCmdClient
   ```

2. **Create a Campaign**
   - Select option `2` (Create Campaign with Natural Language)
   - Enter: `Create a marketing campaign targeting 3 retail companies for our new AI-powered inventory management solution. Include landing pages, emails, and LinkedIn posts.`

3. **Create Campaign Plan**
   - Select option `3` (Create Campaign Plan)
   - Press Enter for default instructions

## Human-in-the-Loop Workflow

4. **Execute Campaign (First Time)**
   - Select option `4` (Execute Campaign)
   - The system will generate company briefs and pause for human approval
   - You'll see output like:
     ```
     ⏳ HUMAN APPROVAL REQUIRED
     The following steps require human review and approval:
       • Generate Company Brief for [Company Name] - Create comprehensive targeting brief
       • Review Company Brief for [Company Name] - Human review and approval
     
     Please use the review commands to approve or modify these steps before continuing.
     Use 'review brief [company name]' to review and approve company briefs.
     ```

5. **Review Company Briefs**
   - Select option `9` (Review Company Briefs)
   - Use the following commands:

   **List Pending Approvals:**
   ```
   list
   ```

   **Review a Company Brief:**
   ```
   review [Company Name]
   ```
   - This displays the full generated brief for review

   **Approve a Brief:**
   ```
   approve [Company Name]
   ```

   **Approve with Modifications:**
   ```
   modify [Company Name] Focus more on their digital transformation needs
   ```

   **Reject a Brief:**
   ```
   reject [Company Name] Insufficient market research for this company
   ```

6. **Continue Campaign Execution**
   - After approving all briefs, use:
     ```
     continue
     ```
   - Or return to main menu and select option `4` (Execute Campaign) again

## Expected Workflow

### Phase 1: Campaign Setup
1. Campaign created with goal and target criteria
2. Plan generated with company brief generation and review steps
3. System identifies target companies (e.g., "FashionForward", "TechCorp", "RetailPro")

### Phase 2: Brief Generation and Review
4. System generates detailed company briefs including:
   - Company overview and metrics
   - Campaign alignment analysis
   - Personalization strategy
   - Risk assessment
   - Success metrics and timeline

5. System pauses and requests human approval

### Phase 3: Human Review Process
6. Human reviews each brief for accuracy and strategy
7. Human can:
   - Approve as-is
   - Approve with modifications
   - Reject and request regeneration

### Phase 4: Content Generation
8. After approval, system continues with content generation
9. Creates personalized landing pages, emails, LinkedIn posts for approved companies
10. Campaign completes with all content generated

## Sample Company Brief Review

When you run `review TechCorp`, you'll see something like:

```markdown
📋 COMPANY BRIEF REVIEW - TechCorp
==================================================

# Company Brief: TechCorp

## Executive Summary
TechCorp is a technology company with 250 employees, generating approximately 
15% annual growth. This brief outlines our strategic approach for engaging 
with them in our "AI-powered inventory management solution" campaign.

## Company Overview
**Company Name**: TechCorp
**Industry**: Technology
**Size**: 250 employees
**Location**: San Francisco, CA
**Website**: https://techcorp.com
**Founded**: 2010

## Financial & Performance Metrics
- **Annual Growth Rate**: 15%
- **Customer Satisfaction**: 8.5/10
- **Market Share**: 12%
- **Active Clients**: 1,200

## Campaign Alignment Analysis
**Our Goal**: AI-powered inventory management solution
**Why TechCorp**: 
- Strong growth trajectory (15%)
- Technology industry alignment
- 250 employees fits our target profile
- Established since 2010

[... more content ...]

==================================================

REVIEW OPTIONS:
• approve brief TechCorp - Approve the brief as-is
• modify brief TechCorp [your feedback] - Approve with modifications  
• reject brief TechCorp [reason] - Reject the brief

Current Status: PendingReview
```

## Testing Different Scenarios

### Scenario 1: Approve All Briefs
- Review and approve all generated briefs
- Continue execution
- Verify all content is generated

### Scenario 2: Modify Briefs
- Review briefs and provide modification feedback
- Continue execution
- Check that feedback is incorporated into execution log

### Scenario 3: Reject and Regenerate
- Reject a brief with specific feedback
- Note how the system handles rejection
- Test regeneration workflow

### Scenario 4: Mixed Approvals
- Approve some briefs, modify others, reject one
- Test that system handles mixed approval states correctly

## Verification Points

1. **Brief Quality**: Check that generated briefs contain relevant company information
2. **Pause Behavior**: Verify execution pauses at approval points
3. **State Persistence**: Confirm approvals are saved and persist across sessions
4. **Feedback Integration**: Check that human feedback is stored and logged
5. **Resume Functionality**: Verify campaign continues correctly after approvals
6. **Content Generation**: Confirm personalized content is generated only for approved companies

## Troubleshooting

### Common Issues

1. **"No active session"**: Start a new campaign first
2. **"Company brief not found"**: Execute the campaign to generate briefs first
3. **"Step not found"**: Ensure you're using the exact company name from the plan

### Debug Commands

- Use `list` to see all pending approvals
- Check campaign status with option `5` from main menu
- View generated content with option `7` from main menu

## Clean Up

To start fresh:
1. Exit the application
2. Delete the `campaign_sessions` folder
3. Restart the application

This removes all saved sessions and allows you to test the workflow from the beginning.
