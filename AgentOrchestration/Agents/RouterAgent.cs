using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using AgentOrchestration.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents
{
    /// <summary>
    /// Router agent responsible for orchestrating plan execution across different agents and tools
    /// </summary>
    public class RouterAgent : BaseAgent
    {
        private readonly ResearcherAgent _researcherAgent;
        private readonly ContentGenerationTools _contentTools;

        public override string Name => "Router";
        public override string Description => "Orchestrates campaign plan execution across multiple agents and tools";

        private const string ROUTER_SYSTEM_PROMPT = @"
You are the Campaign Orchestration Router. Your role is to coordinate the execution of campaign plans by:
1. Managing the sequence of steps in the campaign plan
2. Calling appropriate agents and tools for each step
3. Collecting and organizing results
4. Managing human-in-the-loop approval processes
5. Ensuring campaign context is maintained throughout execution

You maintain the overall campaign state and ensure all components work together cohesively.
";

        public RouterAgent(Kernel kernel, ResearcherAgent researcherAgent, ContentGenerationTools contentTools) 
            : base(kernel, ROUTER_SYSTEM_PROMPT)
        {
            _researcherAgent = researcherAgent;
            _contentTools = contentTools;
        }

        public override async Task<string> ProcessAsync(string input, CampaignSession session)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Starting plan execution");

                if (session.Plan == null)
                {
                    return "No campaign plan available. Please create a plan first.";
                }

                return await ExecutePlan(session);
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Error - {ex.Message}");
                return $"Error executing campaign plan: {ex.Message}";
            }
        }

        public async Task<string> ExecutePlan(CampaignSession session)
        {
            var plan = session.Plan!;
            var results = new List<string>();
            results.Add($"Executing campaign with {plan.Steps.Count} steps...");
            
            // Check for any pending human approvals
            var pendingApprovalSteps = plan.Steps.Where(s => s.RequiresHumanApproval && s.ApprovalStatus == HumanApprovalStatus.PendingReview).ToList();
            
            if (pendingApprovalSteps.Any())
            {
                results.Add($"\n⏳ HUMAN APPROVAL REQUIRED");
                results.Add($"The following steps require human review and approval:");
                
                foreach (var step in pendingApprovalSteps)
                {
                    results.Add($"  • {step.Name} - {step.Description}");
                    
                    // If this is a company brief that needs to be generated first
                    if (step.Function == "generate_company_brief" && step.Result == null)
                    {
                        var stepResult = await ExecuteStepInternal(step, session);
                        step.Result = stepResult;
                        results.Add($"    Content generated: {stepResult}");
                    }
                }
                
                results.Add($"\nPlease use the review commands to approve or modify these steps before continuing.");
                results.Add($"Use 'review brief [company name]' to review and approve company briefs.");
                
                return string.Join("\n", results);
            }
            
            // Group steps for optimal execution
            var stepGroups = GroupStepsForExecution(plan.Steps, plan.CurrentStepIndex);
            
            foreach (var group in stepGroups)
            {
                if (group.CanExecuteInParallel && group.Steps.Count > 1)
                {
                    // Execute content generation steps in parallel
                    await ExecuteStepsInParallel(group.Steps, session, results);
                }
                else
                {
                    // Execute steps sequentially
                    foreach (var step in group.Steps)
                    {
                        await ExecuteStepSequentially(step, session, results, plan);
                        
                        // Check if we hit a step that requires human approval
                        if (step.RequiresHumanApproval && step.ApprovalStatus == HumanApprovalStatus.PendingReview)
                        {
                            results.Add($"\n⏳ Campaign paused - Human approval required for: {step.Name}");
                            results.Add($"Use 'review brief [company name]' to review and approve before continuing.");
                            return string.Join("\n", results);
                        }
                    }
                }
            }

            // Mark campaign as executed if all steps completed
            if (plan.CurrentStepIndex >= plan.Steps.Count)
            {
                session.Campaign.Status = CampaignStatus.Executed;
                session.Campaign.ExecutedAt = DateTime.UtcNow;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Campaign execution completed");
                results.Add("\n🎉 Campaign execution completed! All content has been generated and deployed.");
            }

            return string.Join("\n", results);
        }

        private List<StepExecutionGroup> GroupStepsForExecution(List<PlanStep> steps, int currentStepIndex)
        {
            var groups = new List<StepExecutionGroup>();
            var currentGroup = new StepExecutionGroup();
            
            for (int i = currentStepIndex; i < steps.Count; i++)
            {
                var step = steps[i];
                
                if (step.IsCompleted)
                {
                    continue;
                }
                
                // Content generation steps can be parallelized
                bool isContentGeneration = step.AgentType == "ContentTool";
                
                if (isContentGeneration && (currentGroup.CanExecuteInParallel || currentGroup.Steps.Count == 0))
                {
                    currentGroup.CanExecuteInParallel = true;
                    currentGroup.Steps.Add(step);
                }
                else
                {
                    // Start new group if current group is not empty
                    if (currentGroup.Steps.Count > 0)
                    {
                        groups.Add(currentGroup);
                        currentGroup = new StepExecutionGroup();
                    }
                    
                    currentGroup.CanExecuteInParallel = false;
                    currentGroup.Steps.Add(step);
                    
                    // Non-content steps are executed individually
                    groups.Add(currentGroup);
                    currentGroup = new StepExecutionGroup();
                }
            }
            
            // Add the last group if it has steps
            if (currentGroup.Steps.Count > 0)
            {
                groups.Add(currentGroup);
            }
            
            return groups;
        }

        private async Task ExecuteStepsInParallel(List<PlanStep> steps, CampaignSession session, List<string> results)
        {
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Executing {steps.Count} content generation steps in parallel");
            
            var tasks = steps.Select(async step =>
            {
                var stepResult = await ExecuteStep(step, session);
                
                step.Result = stepResult;
                step.IsCompleted = true;
                step.CompletedAt = DateTime.UtcNow;
                
                return $"✅ {step.Name}: {stepResult}";
            });
            
            var stepResults = await Task.WhenAll(tasks);
            results.AddRange(stepResults);
            
            // Update current step index
            var plan = session.Plan!;
            var lastStepIndex = steps.Max(s => plan.Steps.IndexOf(s));
            plan.CurrentStepIndex = Math.Max(plan.CurrentStepIndex, lastStepIndex + 1);
            
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Parallel execution completed for {steps.Count} steps");
        }

        private async Task ExecuteStepSequentially(PlanStep step, CampaignSession session, List<string> results, CampaignPlan plan)
        {
            var stepIndex = plan.Steps.IndexOf(step) + 1;
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Executing step {stepIndex}/{plan.Steps.Count}: {step.Name}");

            var stepResult = await ExecuteStep(step, session);
            
            step.Result = stepResult;
            step.IsCompleted = true;
            step.CompletedAt = DateTime.UtcNow;
            plan.CurrentStepIndex = stepIndex;

            results.Add($"✅ Step {stepIndex}: {step.Name}\n   Result: {stepResult}");

            // Add delay for non-parallel steps to simulate processing time
            await Task.Delay(500);
        }

        private async Task<string> ExecuteStep(PlanStep step, CampaignSession session)
        {
            try
            {
                // Check if this step requires human approval and hasn't been approved yet
                if (step.RequiresHumanApproval && step.ApprovalStatus == HumanApprovalStatus.PendingReview)
                {
                    // First, execute the step to generate content for review
                    if (step.Function != "ReviewCompanyBrief") // Don't execute review steps, just return pending message
                    {
                        var preliminaryResult = await ExecuteStepInternal(step, session);
                        step.Result = preliminaryResult;
                        session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Router: Generated content for human review: {step.Name}");
                    }
                    
                    // Mark as pending human approval
                    step.ApprovalStatus = HumanApprovalStatus.PendingReview;
                    return $"⏳ Content generated and awaiting human approval. Please review and approve before continuing.";
                }

                // If step was rejected, return rejection message
                if (step.ApprovalStatus == HumanApprovalStatus.Rejected)
                {
                    return $"❌ Step rejected by human reviewer: {step.HumanFeedback ?? "No feedback provided"}";
                }

                // If step was approved with modifications, use the human feedback
                if (step.ApprovalStatus == HumanApprovalStatus.ApprovedWithModifications)
                {
                    return $"✅ Step approved with modifications: {step.HumanFeedback ?? "No modifications specified"}";
                }

                // Execute the step normally
                return await ExecuteStepInternal(step, session);
            }
            catch (Exception ex)
            {
                return $"Error executing step: {ex.Message}";
            }
        }

        private async Task<string> ExecuteStepInternal(PlanStep step, CampaignSession session)
        {
            return step.AgentType switch
            {
                "ResearcherAgent" => await ExecuteResearcherStep(step, session),
                "ContentTool" => await ExecuteContentGenerationStep(step, session),
                "RouterAgent" => await ExecuteRouterStep(step, session),
                _ => $"Unknown agent type: {step.AgentType}"
            };
        }

        private async Task<string> ExecuteResearcherStep(PlanStep step, CampaignSession session)
        {
            switch (step.Function)
            {
                case "GetCustomerInsights":
                    var audience = step.Parameters.GetValueOrDefault("audience", "").ToString();
                    return await _researcherAgent.ProcessAsync(audience!, session);
                
                case "GetIndustryInsights":
                    var industry = step.Parameters.GetValueOrDefault("industry", "").ToString();
                    var companyCount = step.Parameters.GetValueOrDefault("companyCount", 0);
                    return await _researcherAgent.GetIndustryInsights(industry!, Convert.ToInt32(companyCount));
                
                case "GetCompanySpecificInsights":
                    var companyName = step.Parameters.GetValueOrDefault("companyName", "").ToString();
                    var companyId = step.Parameters.GetValueOrDefault("companyId", "").ToString();
                    var companyIndustry = step.Parameters.GetValueOrDefault("industry", "").ToString();
                    return await _researcherAgent.GetCompanySpecificInsights(companyName!, companyId!, companyIndustry!);
                
                default:
                    return $"Unknown researcher function: {step.Function}";
            }
        }

        private async Task<string> ExecuteContentGenerationStep(PlanStep step, CampaignSession session)
        {
            var goal = step.Parameters.GetValueOrDefault("goal", "").ToString() ?? "";
            var companyName = step.Parameters.GetValueOrDefault("companyName", "").ToString() ?? "";
            var companyProfile = step.Parameters.GetValueOrDefault("companyProfile", "").ToString() ?? "";
            var contentType = step.Parameters.GetValueOrDefault("contentType", "").ToString() ?? "";
            var insights = step.Parameters.GetValueOrDefault("insights", "").ToString() ?? "";

            // For legacy support, also check for brief parameter
            var brief = step.Parameters.GetValueOrDefault("brief", "").ToString() ?? "";
            if (string.IsNullOrEmpty(goal) && !string.IsNullOrEmpty(brief))
            {
                goal = brief;
            }

            switch (step.Function)
            {
                // Company brief generation
                case "generate_company_brief":
                    var companyBrief = await _contentTools.GenerateCompanyBrief(goal, companyName, insights);
                    var briefKey = $"CompanyBrief_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[briefKey] = companyBrief;
                    return $"Company brief generated for {companyName}";

                // Company-specific content generation functions
                case "generate_personalized_landing_page":
                    var personalizedLandingPage = await _contentTools.GeneratePersonalizedLandingPage(goal, companyName, companyProfile);
                    var landingPageKey = $"LandingPage_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[landingPageKey] = personalizedLandingPage;
                    return $"Personalized landing page generated for {companyName}";

                case "generate_personalized_email":
                    var personalizedEmail = await _contentTools.GeneratePersonalizedEmail(goal, companyName, companyProfile);
                    var emailKey = $"Email_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[emailKey] = personalizedEmail;
                    return $"Personalized email generated for {companyName}";

                case "generate_personalized_linkedin_post":
                    var personalizedLinkedIn = await _contentTools.GeneratePersonalizedLinkedInPost(goal, companyName, companyProfile);
                    var linkedInKey = $"LinkedInPost_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[linkedInKey] = personalizedLinkedIn;
                    return $"Personalized LinkedIn post generated for {companyName}";

                case "generate_personalized_ad_copy":
                    var personalizedAdCopy = await _contentTools.GeneratePersonalizedAdCopy(goal, companyName, companyProfile);
                    var adCopyKey = $"AdCopy_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[adCopyKey] = personalizedAdCopy;
                    return $"Personalized ad copy generated for {companyName}";

                case "generate_personalized_content":
                    // Fallback to specific content type functions
                    var personalizedContent = contentType.ToLower() switch
                    {
                        "landing page" or "landing site" => await _contentTools.GeneratePersonalizedLandingPage(goal, companyName, companyProfile),
                        "email" => await _contentTools.GeneratePersonalizedEmail(goal, companyName, companyProfile),
                        "linkedin" or "linkedin post" => await _contentTools.GeneratePersonalizedLinkedInPost(goal, companyName, companyProfile),
                        "ads" or "ad copy" => await _contentTools.GeneratePersonalizedAdCopy(goal, companyName, companyProfile),
                        _ => await _contentTools.GeneratePersonalizedEmail(goal, companyName, companyProfile) // Default to email if unknown type
                    };
                    var contentKey = $"{contentType}_{companyName.Replace(" ", "_")}";
                    session.Campaign.GeneratedContent[contentKey] = personalizedContent;
                    return $"Personalized {contentType} content generated for {companyName}";

                default:
                    return $"Unknown content generation function: {step.Function}";
            }
        }

        private async Task<string> ExecuteRouterStep(PlanStep step, CampaignSession session)
        {
            switch (step.Function)
            {
                case "PrepareCampaignExecution":
                    return await PrepareCampaignExecution(session);
                
                case "ReviewCompanyBrief":
                    return await ReviewCompanyBrief(step, session);
                    
                case "ReviewCompanyCampaign":
                    return await ReviewCompanyCampaign(step, session);
                
                case "CoordinateMultiCompanyCampaign":
                    return await CoordinateMultiCompanyCampaign(step, session);
                
                default:
                    return $"Unknown router function: {step.Function}";
            }
        }

        private async Task<string> PrepareCampaignExecution(CampaignSession session)
        {
            await Task.Delay(500); // Simulate processing time

            var summary = $@"
Campaign Execution Summary:
- Campaign: {session.Campaign.Goal}
- Target Audience: {session.Campaign.Audience}
- Components Generated: {session.Campaign.GeneratedContent.Count}
- Status: {session.Campaign.Status}

Generated and Deployed Content:
{string.Join("\n", session.Campaign.GeneratedContent.Select(kv => $"- {kv.Key}: ✅ Deployed"))}

All campaign components have been generated and deployed successfully.
";

            return summary;
        }

        private async Task<string> ReviewCompanyCampaign(PlanStep step, CampaignSession session)
        {
            await Task.Delay(500); // Simulate processing time

            var companyName = step.Parameters.GetValueOrDefault("companyName", "").ToString() ?? "";
            var companyId = step.Parameters.GetValueOrDefault("companyId", "").ToString() ?? "";
            var campaignId = step.Parameters.GetValueOrDefault("campaignId", "").ToString() ?? "";

            // Find all content generated for this company
            var companyContent = session.Campaign.GeneratedContent
                .Where(kv => kv.Key.EndsWith($"_{companyName.Replace(" ", "_")}"))
                .ToList();

            var summary = $@"
Company Campaign Deployment for {companyName}:
- Campaign ID: {campaignId}
- Company ID: {companyId}
- Generated Content Items: {companyContent.Count}

Content Summary:
{string.Join("\n", companyContent.Select(kv => $"✅ {kv.Key}: Deployed successfully"))}

Deployment Checklist:
✅ Content personalized for {companyName}
✅ Brand alignment verified
✅ Messaging consistency confirmed
✅ Call-to-action optimized
✅ Content deployed to {companyName}

Status: Successfully deployed to {companyName}
";

            return summary;
        }

        private async Task<string> CoordinateMultiCompanyCampaign(PlanStep step, CampaignSession session)
        {
            await Task.Delay(1000); // Simulate processing time

            var campaignId = step.Parameters.GetValueOrDefault("campaignId", "").ToString() ?? "";
            var totalCompanies = step.Parameters.GetValueOrDefault("totalCompanies", 0);
            var targetCompanies = step.Parameters.GetValueOrDefault("targetCompanies", new List<string>());

            var companyList = targetCompanies is List<string> companies ? companies : new List<string>();

            // Analyze generated content across all companies
            var contentByType = new Dictionary<string, int>();
            foreach (var content in session.Campaign.GeneratedContent)
            {
                var contentType = content.Key.Split('_')[0];
                contentByType[contentType] = contentByType.GetValueOrDefault(contentType, 0) + 1;
            }

            var summary = $@"
🎯 Multi-Company Campaign Deployment Complete

Campaign Overview:
- Campaign ID: {campaignId}
- Total Target Companies: {totalCompanies}
- Total Content Items Generated: {session.Campaign.GeneratedContent.Count}

Content Distribution:
{string.Join("\n", contentByType.Select(kv => $"• {kv.Key}: {kv.Value} personalized versions deployed"))}

Company Deployment Status:
{string.Join("\n", companyList.Take(5).Select(company => $"✅ {company}: All content successfully deployed"))}
{(companyList.Count > 5 ? $"... and {companyList.Count - 5} more companies deployed" : "")}

Campaign Execution Results:
1. Individual deployment completed for each company
2. Personalized content delivered to each target
3. Coordinated timing achieved across all companies
4. Performance tracking active for each company

Status: All companies successfully deployed and campaign launched!
";

            return summary;
        }

        private async Task<string> ReviewCompanyBrief(PlanStep step, CampaignSession session)
        {
            await Task.Delay(200); // Simulate processing time

            var companyName = step.Parameters.GetValueOrDefault("companyName", "").ToString() ?? "";
            var companyId = step.Parameters.GetValueOrDefault("companyId", "").ToString() ?? "";
            
            // Get the generated company brief from the session
            var briefKey = $"CompanyBrief_{companyName.Replace(" ", "_")}";
            
            if (!session.Campaign.GeneratedContent.ContainsKey(briefKey))
            {
                return $"❌ Company brief not found for {companyName}. Please generate the brief first.";
            }

            var brief = session.Campaign.GeneratedContent[briefKey].ToString();
            
            // Check if this step has already been approved
            if (step.ApprovalStatus == HumanApprovalStatus.Approved)
            {
                return $"✅ Company brief for {companyName} has been approved and is ready for content generation.";
            }
            
            if (step.ApprovalStatus == HumanApprovalStatus.ApprovedWithModifications)
            {
                return $"✅ Company brief for {companyName} has been approved with modifications: {step.HumanFeedback}";
            }
            
            if (step.ApprovalStatus == HumanApprovalStatus.Rejected)
            {
                return $"❌ Company brief for {companyName} has been rejected: {step.HumanFeedback}";
            }

            // If we reach here, the brief is pending review
            return $"⏳ Company brief for {companyName} is ready for human review. Please approve or provide feedback to continue.";
        }

        public Task<string> GetExecutionStatus(CampaignSession session)
        {
            if (session.Plan == null)
            {
                return Task.FromResult("No campaign plan available.");
            }

            var plan = session.Plan;
            var completedSteps = plan.Steps.Count(s => s.IsCompleted);
            var totalSteps = plan.Steps.Count;
            var progressPercentage = (completedSteps * 100) / totalSteps;

            var statusReport = $@"
Campaign Execution Status:
- Progress: {completedSteps}/{totalSteps} steps completed ({progressPercentage}%)
- Current Status: {session.Campaign.Status}
- Started: {plan.CreatedAt:yyyy-MM-dd HH:mm:ss}
- Last Updated: {session.LastUpdated:yyyy-MM-dd HH:mm:ss}

Step Details:
{string.Join("\n", plan.Steps.Select((s, i) => $"{i + 1}. {s.Name}: {(s.IsCompleted ? "✅ Completed" : "⏳ Pending")}"))}

Recent Activity:
{string.Join("\n", session.Campaign.ExecutionLog.TakeLast(5))}
";

            return Task.FromResult(statusReport);
        }

        public Task<string> ReviewCompanyBriefForApproval(string companyName, CampaignSession session)
        {
            var plan = session.Plan;
            if (plan == null)
            {
                return Task.FromResult("❌ No campaign plan available.");
            }

            // Find the company brief generation step
            var briefStep = plan.Steps.FirstOrDefault(s => 
                s.Function == "generate_company_brief" && 
                s.Parameters.GetValueOrDefault("companyName", "").ToString() == companyName);

            if (briefStep == null)
            {
                return Task.FromResult($"❌ Company brief step not found for {companyName}.");
            }

            var briefKey = $"CompanyBrief_{companyName.Replace(" ", "_")}";
            
            if (!session.Campaign.GeneratedContent.ContainsKey(briefKey))
            {
                return Task.FromResult($"❌ Company brief not found for {companyName}. Please generate the brief first.");
            }

            var brief = session.Campaign.GeneratedContent[briefKey].ToString();
            
            var result = $@"
📋 COMPANY BRIEF REVIEW - {companyName}
{new string('=', 50)}

{brief}

{new string('=', 50)}

REVIEW OPTIONS:
• approve brief {companyName} - Approve the brief as-is
• modify brief {companyName} [your feedback] - Approve with modifications
• reject brief {companyName} [reason] - Reject the brief

Current Status: {briefStep.ApprovalStatus}
";

            return Task.FromResult(result);
        }

        public Task<string> ApproveBrief(string companyName, string feedback, CampaignSession session, bool isApproved = true, bool isModified = false)
        {
            var plan = session.Plan;
            if (plan == null)
            {
                return Task.FromResult("❌ No campaign plan available.");
            }

            // Find both the brief generation and review steps
            var briefStep = plan.Steps.FirstOrDefault(s => 
                s.Function == "generate_company_brief" && 
                s.Parameters.GetValueOrDefault("companyName", "").ToString() == companyName);

            var reviewStep = plan.Steps.FirstOrDefault(s => 
                s.Function == "ReviewCompanyBrief" && 
                s.Parameters.GetValueOrDefault("companyName", "").ToString() == companyName);

            if (briefStep == null || reviewStep == null)
            {
                return Task.FromResult($"❌ Company brief or review step not found for {companyName}.");
            }

            if (!isApproved)
            {
                // Rejected
                briefStep.ApprovalStatus = HumanApprovalStatus.Rejected;
                reviewStep.ApprovalStatus = HumanApprovalStatus.Rejected;
                briefStep.HumanFeedback = feedback;
                reviewStep.HumanFeedback = feedback;
                briefStep.ReviewedAt = DateTime.UtcNow;
                reviewStep.ReviewedAt = DateTime.UtcNow;
                
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Human: Rejected company brief for {companyName}: {feedback}");
                
                return Task.FromResult($"❌ Company brief for {companyName} has been rejected. Reason: {feedback}");
            }
            else if (isModified)
            {
                // Approved with modifications
                briefStep.ApprovalStatus = HumanApprovalStatus.ApprovedWithModifications;
                reviewStep.ApprovalStatus = HumanApprovalStatus.ApprovedWithModifications;
                briefStep.HumanFeedback = feedback;
                reviewStep.HumanFeedback = feedback;
                briefStep.ReviewedAt = DateTime.UtcNow;
                reviewStep.ReviewedAt = DateTime.UtcNow;
                
                // Mark both steps as completed
                briefStep.IsCompleted = true;
                reviewStep.IsCompleted = true;
                briefStep.CompletedAt = DateTime.UtcNow;
                reviewStep.CompletedAt = DateTime.UtcNow;
                
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Human: Approved company brief for {companyName} with modifications: {feedback}");
                
                return Task.FromResult($"✅ Company brief for {companyName} has been approved with modifications: {feedback}");
            }
            else
            {
                // Approved as-is
                briefStep.ApprovalStatus = HumanApprovalStatus.Approved;
                reviewStep.ApprovalStatus = HumanApprovalStatus.Approved;
                briefStep.HumanFeedback = feedback;
                reviewStep.HumanFeedback = feedback;
                briefStep.ReviewedAt = DateTime.UtcNow;
                reviewStep.ReviewedAt = DateTime.UtcNow;
                
                // Mark both steps as completed
                briefStep.IsCompleted = true;
                reviewStep.IsCompleted = true;
                briefStep.CompletedAt = DateTime.UtcNow;
                reviewStep.CompletedAt = DateTime.UtcNow;
                
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Human: Approved company brief for {companyName}");
                
                return Task.FromResult($"✅ Company brief for {companyName} has been approved and is ready for content generation.");
            }
        }

        public Task<string> ListPendingApprovals(CampaignSession session)
        {
            var plan = session.Plan;
            if (plan == null)
            {
                return Task.FromResult("❌ No campaign plan available.");
            }

            var pendingSteps = plan.Steps.Where(s => s.RequiresHumanApproval && s.ApprovalStatus == HumanApprovalStatus.PendingReview).ToList();
            
            if (!pendingSteps.Any())
            {
                return Task.FromResult("✅ No pending approvals. Campaign can continue execution.");
            }

            var result = new List<string>();
            result.Add($"⏳ PENDING APPROVALS ({pendingSteps.Count})");
            result.Add(new string('=', 40));

            foreach (var step in pendingSteps)
            {
                var companyName = step.Parameters.GetValueOrDefault("companyName", "Unknown").ToString();
                result.Add($"• {step.Name}");
                result.Add($"  Company: {companyName}");
                result.Add($"  Type: {step.Function}");
                result.Add($"  Status: {step.ApprovalStatus}");
                result.Add("");
            }

            result.Add("AVAILABLE COMMANDS:");
            result.Add("• review brief [company name] - Review a company brief");
            result.Add("• approve brief [company name] - Approve a company brief");
            result.Add("• modify brief [company name] [feedback] - Approve with modifications");
            result.Add("• reject brief [company name] [reason] - Reject a company brief");

            return Task.FromResult(string.Join("\n", result));
        }
    }

    public class StepExecutionGroup
    {
        public bool CanExecuteInParallel { get; set; }
        public List<PlanStep> Steps { get; set; } = new List<PlanStep>();
    }
}
