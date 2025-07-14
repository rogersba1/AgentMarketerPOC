using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentOrchestration.Models;
using AgentOrchestration.Services;
using AgentOrchestration.Agents.Modern;
using AgentOrchestration.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentOrchestration.Services.Modern
{
    /// <summary>
    /// Sequential orchestration service implementing proper SK orchestration patterns
    /// for multi-company campaign execution with human-in-the-loop approval
    /// </summary>
    public class SequentialCampaignOrchestrationService
    {
        private readonly Kernel _kernel;
        private readonly ModernResearcherAgent _researcherAgent;
        private readonly ContentGenerationTools _contentTools;
        private readonly MockCompanyDataService _companyDataService;

        public SequentialCampaignOrchestrationService(Kernel kernel)
        {
            _kernel = kernel;
            _companyDataService = new MockCompanyDataService();
            _researcherAgent = new ModernResearcherAgent(kernel);
            _contentTools = new ContentGenerationTools(kernel, _companyDataService);
            
            // Initialize company data
            _ = _companyDataService.LoadCompanyDataAsync();
        }

        /// <summary>
        /// Execute campaign using proper Sequential Orchestration pattern
        /// Step 1: Company Discovery → Step 2: Brief Generation → Step 3: Human Approval → Step 4: Content Generation
        /// </summary>
        public async Task<string> ExecuteCampaignSequentiallyAsync(CampaignSession session, string userRequest)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Sequential Orchestration: Starting campaign execution");

                // Step 1: Company Discovery and Industry Analysis
                var discoveryResult = await Step1_CompanyDiscoveryAsync(session, userRequest);
                
                // Step 2: Brief Generation (per company)
                var briefResults = await Step2_GenerateCompanyBriefsAsync(session, discoveryResult);
                
                // Step 3: Human-in-the-Loop Approval Points
                var approvalStatus = await Step3_PrepareHumanApprovalAsync(session, briefResults);
                
                // Step 4: Content Generation Setup (ready for execution after approvals)
                var contentPlan = await Step4_PrepareContentGenerationAsync(session, briefResults);

                session.Campaign.Status = CampaignStatus.AwaitingApproval;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Sequential Orchestration: Campaign ready for human approval");

                // Save session with pending approvals
                var contextService = new ContextPersistenceService();
                await contextService.SaveSessionAsync(session);

                return FormatSequentialOrchestrationResult(discoveryResult, briefResults, approvalStatus, contentPlan);
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Sequential Orchestration: Error - {ex.Message}");
                throw new InvalidOperationException($"Sequential orchestration failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Step 1: Company Discovery - Identify target companies from user request
        /// </summary>
        private async Task<CompanyDiscoveryResult> Step1_CompanyDiscoveryAsync(CampaignSession session, string userRequest)
        {
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 1: Company Discovery");

            // Extract industry and company criteria from user request
            var discoveryPrompt = $@"
Analyze this campaign request and identify:
1. Target industry (retail, manufacturing, etc.)
2. Number of companies requested
3. Specific company criteria or preferences
4. Campaign components requested

User Request: {userRequest}
Campaign Goal: {session.Campaign.Goal}
";

            var researchResult = await _researcherAgent.ProcessAsync(discoveryPrompt, session);
            
            // Parse industry from request or default to retail for testing
            var targetIndustry = ExtractIndustryFromRequest(userRequest);
            var requestedCompanyCount = ExtractCompanyCountFromRequest(userRequest);
            
            // Get companies from mock data based on industry
            var availableCompanies = _companyDataService.GetCompaniesByIndustry(targetIndustry);
            var targetCompanies = availableCompanies.Take(requestedCompanyCount).ToList();

            return new CompanyDiscoveryResult
            {
                TargetIndustry = targetIndustry,
                RequestedCount = requestedCompanyCount,
                DiscoveredCompanies = targetCompanies,
                ResearchInsights = researchResult
            };
        }

        /// <summary>
        /// Step 2: Brief Generation - Generate detailed briefs for each discovered company
        /// </summary>
        private async Task<List<CompanyBriefResult>> Step2_GenerateCompanyBriefsAsync(CampaignSession session, CompanyDiscoveryResult discovery)
        {
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 2: Generating briefs for {discovery.DiscoveredCompanies.Count} companies");

            var briefResults = new List<CompanyBriefResult>();

            foreach (var company in discovery.DiscoveredCompanies)
            {
                try
                {
                    // Generate detailed company brief using the restored functionality
                    var brief = await _researcherAgent.GenerateCompanyBrief(
                        session.Campaign.Goal ?? "Drive engagement and growth", 
                        company.CompanyId, 
                        discovery.ResearchInsights);

                    // Store in campaign session
                    StoreCompanyBrief(session, company.CompanyId, brief);

                    briefResults.Add(new CompanyBriefResult
                    {
                        CompanyName = company.BasicInfo.CompanyName,
                        CompanyId = company.CompanyId,
                        Brief = brief,
                        Status = BriefStatus.PendingApproval,
                        GeneratedAt = DateTime.UtcNow
                    });

                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 2: Brief generated for {company.CompanyId}");
                }
                catch (Exception ex)
                {
                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 2: Error generating brief for {company.CompanyId}: {ex.Message}");
                }
            }

            return briefResults;
        }

        /// <summary>
        /// Step 3: Human-in-the-Loop Approval Setup - Prepare briefs for human review
        /// </summary>
        private async Task<ApprovalStatus> Step3_PrepareHumanApprovalAsync(CampaignSession session, List<CompanyBriefResult> briefResults)
        {
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 3: Preparing human approval for {briefResults.Count} company briefs");

            // Create approval workflow for each company brief
            var approvalTasks = new List<ApprovalTask>();
            
            foreach (var briefResult in briefResults)
            {
                approvalTasks.Add(new ApprovalTask
                {
                    CompanyId = briefResult.CompanyId,
                    TaskType = "Company Brief Review",
                    Description = $"Review and approve the company brief for {briefResult.CompanyName}",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    BriefContent = briefResult.Brief
                });
            }

            // Store approval tasks in session (convert to strings for storage)
            session.Campaign.PendingApprovals = approvalTasks.Select(t => $"{t.CompanyId}: {t.BriefContent}").ToList();

            await Task.Delay(100); // Simulate preparation time

            return new ApprovalStatus
            {
                TotalBriefs = briefResults.Count,
                PendingApprovals = approvalTasks.Count,
                ApprovalTasks = approvalTasks,
                Status = "Ready for Human Review"
            };
        }

        /// <summary>
        /// Step 4: Content Generation Setup - Prepare content generation plan based on user request
        /// </summary>
        private async Task<ContentGenerationPlan> Step4_PrepareContentGenerationAsync(CampaignSession session, List<CompanyBriefResult> briefResults)
        {
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 4: Preparing content generation plan");

            // Extract requested components from campaign
            var requestedComponents = session.Campaign.Components ?? new List<string>();
            
            // Map to content generation tools
            var contentTasks = new List<ContentTask>();
            
            foreach (var briefResult in briefResults)
            {
                foreach (var component in requestedComponents)
                {
                    contentTasks.Add(new ContentTask
                    {
                        CompanyId = briefResult.CompanyId,
                        ContentType = component,
                        Status = "Awaiting Brief Approval",
                        ToolFunction = MapComponentToToolFunction(component),
                        DependsOnApproval = true
                    });
                }
            }

            await Task.Delay(100); // Simulate planning time

            return new ContentGenerationPlan
            {
                TotalTasks = contentTasks.Count,
                TasksPerCompany = requestedComponents.Count,
                ContentTasks = contentTasks,
                Status = "Ready for Execution (After Approvals)"
            };
        }

        /// <summary>
        /// Execute content generation for approved companies
        /// </summary>
        public async Task<string> ExecuteContentGenerationAsync(CampaignSession session, List<string> approvedCompanies)
        {
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Executing content generation for {approvedCompanies.Count} approved companies");

            var results = new List<string>();

            foreach (var companyName in approvedCompanies)
            {
                var company = session.Campaign.Companies.FirstOrDefault(c => c.CompanyName == companyName);
                if (company?.Brief == null) continue;

                foreach (var component in session.Campaign.Components ?? new List<string>())
                {
                    try
                    {
                        var contentResult = await GenerateContentForCompany(companyName, component, company.Brief, session.Campaign.Goal);
                        results.Add($"✅ {component} generated for {companyName}");
                        session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Content: {component} generated for {companyName}");
                    }
                    catch (Exception ex)
                    {
                        results.Add($"❌ {component} failed for {companyName}: {ex.Message}");
                        session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Content Error: {component} failed for {companyName}");
                    }
                }
            }

            session.Campaign.Status = CampaignStatus.Executed;
            session.Campaign.ExecutedAt = DateTime.UtcNow;

            return string.Join("\n", results);
        }

        /// <summary>
        /// Get session by ID from context persistence service
        /// </summary>
        public async Task<CampaignSession?> GetSessionAsync(string sessionId)
        {
            var contextService = new ContextPersistenceService();
            return await contextService.LoadSessionAsync(sessionId);
        }

        /// <summary>
        /// Approve a company brief
        /// </summary>
        public async Task<string> ApproveCompanyBriefAsync(string sessionId, string companyId, string feedback = "")
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);
            
            if (session == null)
                throw new ArgumentException($"Session {sessionId} not found");

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Company brief approved for {companyId}: {feedback}");
            
            // Update approval status
            if (session.Campaign.PendingApprovals?.Any() == true)
            {
                var pendingApproval = session.Campaign.PendingApprovals.FirstOrDefault(a => a.Contains(companyId));
                if (pendingApproval != null)
                {
                    session.Campaign.PendingApprovals.Remove(pendingApproval);
                }
            }

            await contextService.SaveSessionAsync(session);
            return $"Company brief for {companyId} has been approved. {feedback}";
        }

        /// <summary>
        /// Approve a company brief with modifications
        /// </summary>
        public async Task<string> ApproveCompanyBriefWithModificationsAsync(string sessionId, string companyId, string modifiedContent, string feedback = "")
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);
            
            if (session == null)
                throw new ArgumentException($"Session {sessionId} not found");

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Company brief approved with modifications for {companyId}: {feedback}");
            
            // Update the brief content with modifications
            if (session.Campaign.PendingApprovals?.Any() == true)
            {
                var pendingApproval = session.Campaign.PendingApprovals.FirstOrDefault(a => a.Contains(companyId));
                if (pendingApproval != null)
                {
                    session.Campaign.PendingApprovals.Remove(pendingApproval);
                    // Add the modified version
                    session.Campaign.PendingApprovals.Add($"{companyId}: {modifiedContent}");
                }
            }

            await contextService.SaveSessionAsync(session);
            return $"Company brief for {companyId} has been approved with modifications. {feedback}";
        }

        /// <summary>
        /// Reject a company brief
        /// </summary>
        public async Task<string> RejectCompanyBriefAsync(string sessionId, string companyId, string reason = "")
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);
            
            if (session == null)
                throw new ArgumentException($"Session {sessionId} not found");

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Company brief rejected for {companyId}: {reason}");
            
            await contextService.SaveSessionAsync(session);
            return $"Company brief for {companyId} has been rejected. Reason: {reason}";
        }

        /// <summary>
        /// Get pending company briefs for approval
        /// </summary>
        public async Task<List<object>> GetPendingCompanyBriefs(string sessionId)
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);
            
            if (session == null)
                throw new ArgumentException($"Session {sessionId} not found");

            var briefs = new List<object>();
            
            // Get briefs from the actual campaign companies data
            if (session.Campaign.Companies?.Any() == true)
            {
                foreach (var company in session.Campaign.Companies)
                {
                    briefs.Add(new
                    {
                        CompanyId = company.CompanyId,
                        CompanyName = company.CompanyName,
                        Content = company.Brief, // Map Brief property to Content for frontend
                        Brief = company.Brief,
                        Industry = ExtractIndustryFromBrief(company.Brief), // Extract industry from brief content
                        CampaignId = sessionId,
                        Status = "Pending",
                        GeneratedAt = company.CreatedAt,
                        KeyMessages = new List<string>(),
                        TargetAudience = ExtractTargetAudienceFromBrief(company.Brief),
                        EstimatedBudget = 0,
                        ProjectedReach = 0
                    });
                }
            }
            
            // Fallback: Check legacy PendingApprovals if no companies found
            else if (session.Campaign.PendingApprovals?.Any() == true)
            {
                foreach (var approval in session.Campaign.PendingApprovals)
                {
                    var parts = approval.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        briefs.Add(new
                        {
                            CompanyId = parts[0].Trim(),
                            CompanyName = parts[0].Trim(),
                            Content = parts[1].Trim(),
                            Brief = parts[1].Trim(),
                            Industry = "Unknown",
                            CampaignId = sessionId,
                            Status = "Pending",
                            GeneratedAt = DateTime.Now,
                            KeyMessages = new List<string>(),
                            TargetAudience = "",
                            EstimatedBudget = 0,
                            ProjectedReach = 0
                        });
                    }
                }
            }

            return briefs;
        }

        private static string ExtractIndustryFromBrief(string brief)
        {
            // Extract industry from brief content - look for "Industry": pattern
            var lines = brief.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains("**Industry**:"))
                {
                    var parts = line.Split(':', 2);
                    if (parts.Length == 2)
                        return parts[1].Trim().TrimStart('*').Trim();
                }
            }
            return "Retail"; // Default fallback
        }

        private static string ExtractTargetAudienceFromBrief(string brief)
        {
            // Extract target audience info from brief content
            if (brief.Contains("decision makers"))
                return "Decision makers and executives";
            return "Business stakeholders";
        }

        /// <summary>
        /// Continue campaign execution after approvals
        /// </summary>
        public async Task<string> ContinueCampaignExecutionAsync(string sessionId)
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);
            
            if (session == null)
                throw new ArgumentException($"Session {sessionId} not found");

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Continuing campaign execution after approvals");

            // Check if there are any pending approvals
            if (session.Campaign.PendingApprovals?.Any() == true)
            {
                return "Cannot continue - there are still pending approvals. Please approve or reject all company briefs first.";
            }

            // Execute content generation for approved companies
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Starting content generation phase");

            await contextService.SaveSessionAsync(session);
            return "Campaign execution continued. Starting content generation for approved companies.";
        }
        // Helper methods
        private string ExtractIndustryFromRequest(string request)
        {
            var lowerRequest = request.ToLower();
            if (lowerRequest.Contains("retail")) return "Retail";
            if (lowerRequest.Contains("manufacturing")) return "Manufacturing";
            if (lowerRequest.Contains("technology") || lowerRequest.Contains("tech")) return "Technology";
            
            return "Retail"; // Default for testing
        }

        private int ExtractCompanyCountFromRequest(string request)
        {
            // Extract numbers from request, default to 3 companies
            var words = request.Split(' ');
            foreach (var word in words)
            {
                if (int.TryParse(word, out int count) && count > 0 && count <= 10)
                {
                    return count;
                }
            }
            return 3; // Default
        }

        private string MapComponentToToolFunction(string component)
        {
            return component.ToLower() switch
            {
                "landing page" => "generate_personalized_landing_page",
                "email" => "generate_personalized_email",
                "linkedin post" => "generate_personalized_linkedin_post",
                "ad" => "generate_personalized_ad",
                _ => "generate_personalized_content"
            };
        }

        private async Task<string> GenerateContentForCompany(string companyId, string component, string brief, string campaignGoal)
        {
            // Use existing ContentGenerationTools
            return component.ToLower() switch
            {
                "landing page" => await _contentTools.GeneratePersonalizedLandingPage(campaignGoal, companyId, brief),
                "email" => await _contentTools.GeneratePersonalizedEmail(campaignGoal, companyId, brief),
                "linkedin post" => await _contentTools.GeneratePersonalizedLinkedInPost(campaignGoal, companyId, brief),
                "ad" => await _contentTools.GeneratePersonalizedAdCopy(campaignGoal, companyId, brief),
                _ => $"Content generated for {companyId}: {component}"
            };
        }

        private void StoreCompanyBrief(CampaignSession session, string companyId, string brief)
        {
            var campaignCompany = session.Campaign.Companies.FirstOrDefault(c => c.CompanyId == companyId);
            
            if (campaignCompany == null)
            {
                campaignCompany = new CampaignCompany
                {
                    CompanyId = companyId,
                    CompanyName = "ABC Default",
                    CreatedAt = DateTime.UtcNow
                };
                session.Campaign.Companies.Add(campaignCompany);
            }
            
            campaignCompany.Brief = brief;
            campaignCompany.LastUpdated = DateTime.UtcNow;
        }

        private string FormatSequentialOrchestrationResult(
            CompanyDiscoveryResult discovery,
            List<CompanyBriefResult> briefs,
            ApprovalStatus approvals,
            ContentGenerationPlan contentPlan)
        {
            return $@"
🎯 **Sequential Campaign Orchestration Complete!**

## Step 1: Company Discovery ✅
- **Target Industry**: {discovery.TargetIndustry}
- **Companies Discovered**: {discovery.DiscoveredCompanies.Count}
- **Companies**: {string.Join(", ", discovery.DiscoveredCompanies.Select(c => c.BasicInfo.CompanyName))}

## Step 2: Brief Generation ✅
- **Briefs Generated**: {briefs.Count}
- **Status**: All briefs ready for review

## Step 3: Human-in-the-Loop Approval 📋
- **Pending Approvals**: {approvals.PendingApprovals}
- **Status**: {approvals.Status}
- **Action Required**: Review and approve company briefs before content generation

## Step 4: Content Generation Plan 📝
- **Total Content Tasks**: {contentPlan.TotalTasks}
- **Tasks Per Company**: {contentPlan.TasksPerCompany}
- **Status**: {contentPlan.Status}

**Next Steps**: 
1. Review each company brief
2. Approve or request modifications
3. Content generation will execute for approved companies

**Sequential Orchestration Features Used:**
✅ Modern ChatCompletionAgent patterns
✅ Step-by-step workflow processing
✅ Human-in-the-loop approval points
✅ Per-company execution planning
✅ Existing ContentGenerationTools integration
";
        }
    }

    // Supporting data models
    public class CompanyDiscoveryResult
    {
        public string TargetIndustry { get; set; } = "";
        public int RequestedCount { get; set; }
        public List<CompanyProfile> DiscoveredCompanies { get; set; } = new();
        public string ResearchInsights { get; set; } = "";
    }

    public class CompanyBriefResult
    {
        public string CompanyName { get; set; } = "";
        public string CompanyId { get; set; } = "";
        public string Brief { get; set; } = "";
        public BriefStatus Status { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class ApprovalStatus
    {
        public int TotalBriefs { get; set; }
        public int PendingApprovals { get; set; }
        public List<ApprovalTask> ApprovalTasks { get; set; } = new();
        public string Status { get; set; } = "";
    }

    public class ContentGenerationPlan
    {
        public int TotalTasks { get; set; }
        public int TasksPerCompany { get; set; }
        public List<ContentTask> ContentTasks { get; set; } = new();
        public string Status { get; set; } = "";
    }

    public class ApprovalTask
    {
        public string CompanyId { get; set; } = "";
        public string TaskType { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string BriefContent { get; set; } = "";
    }

    public class ContentTask
    {
        public string CompanyId { get; set; } = "";
        public string ContentType { get; set; } = "";
        public string Status { get; set; } = "";
        public string ToolFunction { get; set; } = "";
        public bool DependsOnApproval { get; set; }
    }

    public enum BriefStatus
    {
        PendingApproval,
        Approved,
        Rejected,
        Modified
    }
}
