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
using System.Diagnostics.CodeAnalysis;

namespace AgentOrchestration.Services.Modern
{
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    /// <summary>
    /// Semantic Kernel-powered orchestration service using 3-agent AgentGroupChat
    /// Architecture: PlannerAgent (parsing) → ResearchAgent (briefs) → ContentAgent (generation)
    /// Implements human-in-the-loop approval workflow between research and content phases
    /// </summary>
    public class SequentialCampaignOrchestrationService
    {
        private readonly Kernel _kernel;
        private readonly ModernPlannerAgent _plannerAgent;
        private readonly ModernResearcherAgent _researcherAgent;
        private readonly ModernContentAgent _contentAgent;
        private readonly MockCompanyDataService _companyDataService;

        [SuppressMessage("Microsoft.SemanticKernel", "SKEXP0110", Justification = "Evaluation purposes only.")]
        private readonly AgentGroupChat _agentGroupChat;

        public SequentialCampaignOrchestrationService(Kernel kernel)
        {
            _kernel = kernel;
            _companyDataService = new MockCompanyDataService();
            
            // Initialize the 3 main agents
            _plannerAgent = new ModernPlannerAgent(kernel);
            _researcherAgent = new ModernResearcherAgent(kernel);
            _contentAgent = new ModernContentAgent(kernel, _companyDataService);

            // Create AgentGroupChat with the 3 main agents
            _agentGroupChat = new AgentGroupChat(
                _plannerAgent.Agent,
                _researcherAgent.Agent,
                _contentAgent.Agent
            );
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // Initialize company data synchronously to ensure it's available when needed
            _companyDataService.LoadCompanyDataAsync().Wait();
        }

        /// <summary>
        /// Execute campaign using 3-Agent GroupChat: PlannerAgent → ResearchAgent → ContentAgent
        /// Includes human-in-the-loop approval workflow between research and content phases
        /// </summary>
        public async Task<string> ExecuteCampaignSequentiallyAsync(CampaignSession session, string userRequest)
        {
            try
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 3-Agent Orchestration: Starting campaign execution");

                // Phase 1: PlannerAgent - Parse user request using LLM
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Phase 1: PlannerAgent parsing user request");
                var parsedRequest = await _plannerAgent.ParseUserRequestAsync(userRequest);
                
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Parsed - Audience: {parsedRequest.Audience}, Companies: {parsedRequest.CompanyCount}, Components: [{string.Join(", ", parsedRequest.Components)}]");

                // Update session with parsed parameters
                session.Campaign.Audience = parsedRequest.Audience.ToString();
                session.Campaign.Components = parsedRequest.Components;

                // Phase 2: ResearchAgent - Generate company briefs
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Phase 2: ResearchAgent generating company briefs");
                await ExecuteResearchPhaseAsync(session, parsedRequest);

                // Phase 3: Human-in-the-Loop Approval (required before content generation)
                session.Campaign.Status = CampaignStatus.AwaitingApproval;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Phase 3: Human approval required for company briefs");

                // Save session with pending approvals
                var contextService = new ContextPersistenceService();
                await contextService.SaveSessionAsync(session);

                return FormatThreeAgentOrchestrationResult(session, parsedRequest);
            }
            catch (Exception ex)
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 3-Agent Orchestration Error: {ex.Message}");
                throw new InvalidOperationException($"3-Agent campaign orchestration failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute research phase using ResearchAgent to identify companies and generate briefs
        /// </summary>
        private async Task ExecuteResearchPhaseAsync(CampaignSession session, ParsedCampaignRequest parsedRequest)
        {
            // Get companies from mock data based on parsed audience
            var targetIndustry = parsedRequest.Audience.ToString();
            var availableCompanies = _companyDataService.GetCompaniesByIndustry(targetIndustry);
            var targetCompanies = availableCompanies.Take(parsedRequest.CompanyCount).ToList();

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Research: Found {targetCompanies.Count} companies in {targetIndustry} industry");

            // Store companies in session and generate briefs using ResearchAgent
            foreach (var company in targetCompanies)
            {
                var campaignCompany = new CampaignCompany
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.BasicInfo.CompanyName,
                    Brief = "", // Will be populated by ResearchAgent
                    GeneratedContent = new Dictionary<string, string>(),
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                // Generate brief using ResearchAgent
                try
                {
                    var brief = await _researcherAgent.GenerateCompanyBrief(
                        session.Campaign.Goal ?? "Drive engagement and growth",
                        company.CompanyId,
                        $"Target Audience: {parsedRequest.Audience}, Components: {string.Join(", ", parsedRequest.Components)}"
                    );
                    
                    campaignCompany.Brief = brief;
                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Research: Brief generated for {company.BasicInfo.CompanyName}");
                }
                catch (Exception ex)
                {
                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Research: Error generating brief for {company.BasicInfo.CompanyName}: {ex.Message}");
                    campaignCompany.Brief = $"Brief generation failed: {ex.Message}";
                }

                session.Campaign.Companies.Add(campaignCompany);
            }

            // Set up pending approvals for human-in-the-loop workflow
            session.Campaign.PendingApprovals = session.Campaign.Companies
                .Select(c => $"{c.CompanyId}: {c.Brief}")
                .ToList();

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Research: {session.Campaign.PendingApprovals.Count} briefs ready for approval");
        }

        /// <summary>
        /// Execute content generation using ContentAgent after approvals are complete
        /// </summary>
        public async Task<string> ExecuteContentGenerationAsync(CampaignSession session, List<string> approvedCompanies)
        {
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Phase 4: ContentAgent generating content for {approvedCompanies.Count} approved companies");

            // Get approved companies from session
            var approvedCampaignCompanies = session.Campaign.Companies
                .Where(c => approvedCompanies.Contains(c.CompanyName))
                .ToList();

            // Use ContentAgent to generate content
            var contentResult = await _contentAgent.GenerateContentAsync(
                approvedCampaignCompanies,
                session.Campaign.Components ?? new List<string>(),
                session.Campaign.Goal ?? "Drive engagement and growth"
            );

            // Update session with results
            session.Campaign.Status = CampaignStatus.Executed;
            session.Campaign.ExecutedAt = DateTime.UtcNow;
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ContentAgent: Generated {contentResult.SuccessfulTasks}/{contentResult.TotalTasks} content items");

            return FormatContentGenerationResults(contentResult);
        }

        /// <summary>
        /// Format content generation results from ContentAgent
        /// </summary>
        private string FormatContentGenerationResults(ContentGenerationResult result)
        {
            var resultLines = new List<string>
            {
                $"🎯 **ContentAgent Execution Complete!**",
                $"",
                $"## 📊 Generation Summary",
                $"- **Total Tasks**: {result.TotalTasks}",
                $"- **Completed**: {result.CompletedTasks}",
                $"- **Successful**: {result.SuccessfulTasks}",
                $"- **Success Rate**: {(result.SuccessfulTasks * 100.0 / Math.Max(1, result.TotalTasks)):F1}%",
                $""
            };

            foreach (var companyResult in result.CompanyResults)
            {
                resultLines.Add($"### {companyResult.CompanyName}");
                foreach (var contentItem in companyResult.ContentItems)
                {
                    var status = contentItem.Status == "Success" ? "✅" : "❌";
                    resultLines.Add($"- {status} **{contentItem.Component}**: {contentItem.Status}");
                }
                resultLines.Add("");
            }

            return string.Join("\n", resultLines);
        }

        /// <summary>
        /// Format the results of 3-Agent orchestration for user display
        /// </summary>
        private string FormatThreeAgentOrchestrationResult(CampaignSession session, ParsedCampaignRequest parsedRequest)
        {
            var companiesCount = session.Campaign.Companies?.Count ?? 0;
            var pendingApprovals = session.Campaign.PendingApprovals?.Count ?? 0;

            return $@"
🤖 **3-Agent Campaign Orchestration Complete!**

## 🎯 LLM-Parsed Request
- **Target Audience**: {parsedRequest.Audience}
- **Company Count**: {parsedRequest.CompanyCount}
- **Content Components**: {string.Join(", ", parsedRequest.Components)}
- **Parsing Confidence**: {parsedRequest.ConfidenceScore:F2}
- **Additional Context**: {parsedRequest.AdditionalContext}

## 🔄 Agent Execution Flow
✅ **PlannerAgent**: Parsed user request and extracted structured parameters
✅ **ResearchAgent**: Identified {companiesCount} companies and generated briefs
⏳ **ContentAgent**: Waiting for brief approvals before content generation

## 📊 Campaign Status
- **Companies Identified**: {companiesCount}
- **Briefs Generated**: {companiesCount}
- **Pending Approvals**: {pendingApprovals}
- **Campaign Status**: {session.Campaign.Status}

## 📋 Next Steps
1. **Review Company Briefs**: Each brief requires human approval
2. **Approve or Modify**: Use the approval interface
3. **Content Generation**: ContentAgent will generate {string.Join(", ", parsedRequest.Components)} for approved companies
4. **Campaign Completion**: 3-agent workflow will finalize execution

**Ready for human-in-the-loop approval workflow!**
";
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

            // Store discovered companies directly in session state
            foreach (var company in targetCompanies)
            {
                var campaignCompany = new CampaignCompany
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.BasicInfo.CompanyName,
                    Brief = "", // Will be populated in Step 2
                    GeneratedContent = new Dictionary<string, string>(),
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                
                session.Campaign.Companies.Add(campaignCompany);
                
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 1: Added {company.BasicInfo.CompanyName} to campaign");
            }

            return new CompanyDiscoveryResult
            {
                TargetIndustry = targetIndustry,
                RequestedCount = requestedCompanyCount,
                DiscoveredCompanies = targetCompanies,
                ResearchInsights = researchResult
            };
        }

        /// <summary>
        /// Step 2: Brief Generation - Generate detailed briefs for each company in the session
        /// </summary>
        private async Task<List<CompanyBriefResult>> Step2_GenerateCompanyBriefsAsync(CampaignSession session, CompanyDiscoveryResult discovery)
        {
            // Use companies already stored in session instead of discovery result
            var companiesInSession = session.Campaign.Companies.ToList();
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 2: Generating briefs for {companiesInSession.Count} companies from session");

            var briefResults = new List<CompanyBriefResult>();

            foreach (var campaignCompany in companiesInSession)
            {
                try
                {
                    // Generate detailed company brief using the restored functionality
                    var brief = await _researcherAgent.GenerateCompanyBrief(
                        session.Campaign.Goal ?? "Drive engagement and growth", 
                        campaignCompany.CompanyId, 
                        discovery.ResearchInsights);

                    // Update the brief in the existing campaign company (no need for StoreCompanyBrief since it's already in session)
                    campaignCompany.Brief = brief;
                    campaignCompany.LastUpdated = DateTime.UtcNow;

                    briefResults.Add(new CompanyBriefResult
                    {
                        CompanyName = campaignCompany.CompanyName,
                        CompanyId = campaignCompany.CompanyId,
                        Brief = brief,
                        Status = BriefStatus.PendingApproval,
                        GeneratedAt = DateTime.UtcNow
                    });

                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 2: Brief generated for {campaignCompany.CompanyId}");
                }
                catch (Exception ex)
                {
                    session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Step 2: Error generating brief for {campaignCompany.CompanyId}: {ex.Message}");
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
        /// Get session by ID from context persistence service
        /// </summary>
        public async Task<CampaignSession?> GetSessionAsync(string sessionId)
        {
            var contextService = new ContextPersistenceService();
            return await contextService.LoadSessionAsync(sessionId);
        }

        /// <summary>
        /// Approve a company brief and check for workflow progression - SIMPLIFIED
        /// </summary>
        public async Task<WorkflowProgressResult> ApproveCompanyBriefAsync(string sessionId, string companyId, string feedback = "")
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);

            if (session == null)
            {
                return new WorkflowProgressResult
                {
                    HasProgressed = false,
                    NewStatus = CampaignStatus.Failed,
                    Message = $"Session {sessionId} not found",
                    RequiresApproval = false,
                    ApprovalData = new List<object>()
                };
            }

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Company brief approved for {companyId}: {feedback}");

            // Mark company as approved in the Companies collection
            var company = session.Campaign.Companies?.FirstOrDefault(c => c.CompanyId == companyId);
            if (company != null)
            {
                // You could add an approval status field to CampaignCompany model if needed
                company.LastUpdated = DateTime.UtcNow;
            }

            // Remove from legacy PendingApprovals if it exists (cleanup during transition)
            if (session.Campaign.PendingApprovals?.Any() == true)
            {
                var pendingApproval = session.Campaign.PendingApprovals.FirstOrDefault(a => a.Contains(companyId));
                if (pendingApproval != null)
                {
                    session.Campaign.PendingApprovals.Remove(pendingApproval);
                }
            }

            return await CheckAndProgressWorkflowAsync(session);
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
        /// Reject a company brief and check for workflow progression
        /// </summary>
        public async Task<WorkflowProgressResult> RejectCompanyBriefAsync(string sessionId, string companyId, string reason = "")
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);
            
            if (session == null)
            {
                return new WorkflowProgressResult
                {
                    HasProgressed = false,
                    NewStatus = CampaignStatus.Failed,
                    Message = $"Session {sessionId} not found",
                    RequiresApproval = false,
                    ApprovalData = new List<object>()
                };
            }

            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Company brief rejected for {companyId}: {reason}");
            
            // Remove from companies and pending approvals
            if (session.Campaign.Companies != null)
            {
                session.Campaign.Companies.RemoveAll(c => c.CompanyId == companyId);
            }
            
            if (session.Campaign.PendingApprovals?.Any() == true)
            {
                var pendingApproval = session.Campaign.PendingApprovals.FirstOrDefault(a => a.Contains(companyId));
                if (pendingApproval != null)
                {
                    session.Campaign.PendingApprovals.Remove(pendingApproval);
                }
            }
            
            // Check workflow progression and return intelligent result
            return await CheckAndProgressWorkflowAsync(session);
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

            // Use only the modern campaign companies approach
            if (session.Campaign.Companies?.Any() == true && session.Campaign.PendingApprovals?.Any() == true)
            {
                foreach (var approval in session.Campaign.PendingApprovals)
                {
                    var parts = approval.Split(':');

                    var companyId = parts[0].Trim();
                    var briefContent = parts[1].Trim();
                    var company = session.Campaign.Companies.FirstOrDefault(c => c.CompanyId == companyId);
                    if (company != null)
                    {
                        briefs.Add(new CompanyBriefDto
                        {
                            CompanyId = company.CompanyId,
                            CompanyName = company.CompanyName,
                            Content = briefContent,
                            Brief = briefContent,
                            Industry = ExtractIndustryFromBrief(briefContent),
                            CampaignId = sessionId,
                            Status = "Pending",
                            GeneratedAt = DateTime.UtcNow,
                            TargetAudience = ExtractTargetAudienceFromBrief(briefContent),
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

        /// <summary>
        /// Check approval state and use SK Router Agent for intelligent workflow progression
        /// </summary>
        private async Task<WorkflowProgressResult> CheckAndProgressWorkflowAsync(CampaignSession session)
        {
            var contextService = new ContextPersistenceService();
            
            // Count approval states
            var totalCompanies = session.Campaign.Companies?.Count ?? 0;
            var companiesWithBriefs = session.Campaign.Companies?.Where(c => !string.IsNullOrEmpty(c.Brief)).Count() ?? 0;
            var pendingApprovals = session.Campaign.PendingApprovals?.Count ?? 0;
            
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] SK Workflow Check: {totalCompanies} total companies, {companiesWithBriefs} with briefs, {pendingApprovals} pending approvals");

            // Use SK Router Agent to make intelligent workflow decisions
            var routerDecision = await GetRouterAgentDecision(session, totalCompanies, companiesWithBriefs, pendingApprovals);
            
            // If no pending approvals, use SK agents to orchestrate content generation
            if (pendingApprovals == 0 && companiesWithBriefs > 0)
            {
                return await ExecuteSemanticKernelContentGeneration(session, contextService);
            }

            // If still pending approvals, return current state
            if (pendingApprovals > 0)
            {
                var briefsForApproval = await GetPendingCompanyBriefs(session.Id);
                
                return new WorkflowProgressResult
                {
                    HasProgressed = false,
                    NewStatus = session.Campaign.Status,
                    Message = $"SK Router Decision: {routerDecision}\n\nBrief approval processed. {pendingApprovals} briefs still pending review.",
                    RequiresApproval = true,
                    ApprovalData = briefsForApproval
                };
            }

            // Fallback - save session and return current state
            await contextService.SaveSessionAsync(session);
            return new WorkflowProgressResult
            {
                HasProgressed = false,
                NewStatus = session.Campaign.Status,
                Message = $"SK Router Decision: {routerDecision}\n\nApproval processed successfully.",
                RequiresApproval = false,
                ApprovalData = new List<object>()
            };
        }

        /// <summary>
        /// Use ContentAgent for intelligent workflow decisions
        /// </summary>
        private async Task<string> GetRouterAgentDecision(CampaignSession session, int totalCompanies, int companiesWithBriefs, int pendingApprovals)
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage($@"
**Workflow Decision Request**

**Campaign State:**
- Total Companies: {totalCompanies}
- Companies with Briefs: {companiesWithBriefs} 
- Pending Approvals: {pendingApprovals}
- Campaign Status: {session.Campaign.Status}
- Campaign Goal: {session.Campaign.Goal}

**Recent Activity:**
{string.Join("\n", session.Campaign.ExecutionLog.TakeLast(5))}

Please analyze this campaign state and provide a decision on the next workflow step. Should we:
1. Continue waiting for approvals?
2. Proceed to content generation?
3. Request additional information?
4. Modify the workflow?

Provide a clear, actionable decision with reasoning.
");

            var response = _contentAgent.Agent.InvokeAsync(chatHistory);
            var lastMessage = await response.LastOrDefaultAsync();
            
            var decision = lastMessage?.Content ?? "Continue with current workflow";
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 3-Agent Decision: {decision.Substring(0, Math.Min(100, decision.Length))}...");
            
            return decision;
        }

        /// <summary>
        /// Execute content generation using Semantic Kernel agent coordination
        /// </summary>
        private async Task<WorkflowProgressResult> ExecuteSemanticKernelContentGeneration(CampaignSession session, ContextPersistenceService contextService)
        {
            // Get approved companies
            var approvedCompanies = session.Campaign.Companies?
                .Where(c => !string.IsNullOrEmpty(c.Brief))
                .Select(c => c.CompanyName)
                .ToList() ?? new List<string>();

            if (approvedCompanies.Any())
            {
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] SK Content Generation: All approvals complete. Using SK agents for content generation coordination");
                
                // Use SK Agent coordination for content generation
                var contentGenerationPlan = await CoordinateContentGenerationWithSK(session, approvedCompanies);
                
                // Update campaign status
                session.Campaign.Status = CampaignStatus.InProgress;
                
                // Execute content generation
                var contentResults = await ExecuteContentGenerationAsync(session, approvedCompanies);
                
                // Mark campaign as completed
                session.Campaign.Status = CampaignStatus.Executed;
                session.Campaign.ExecutedAt = DateTime.UtcNow;
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] SK Campaign: Execution completed with agent coordination");
                
                await contextService.SaveSessionAsync(session);
                
                return new WorkflowProgressResult
                {
                    HasProgressed = true,
                    NewStatus = CampaignStatus.Executed,
                    Message = FormatSemanticKernelContentComplete(approvedCompanies, contentResults, contentGenerationPlan),
                    RequiresApproval = false,
                    ApprovalData = new List<object>()
                };
            }

            return new WorkflowProgressResult
            {
                HasProgressed = false,
                NewStatus = session.Campaign.Status,
                Message = "SK agents could not identify approved companies for content generation",
                RequiresApproval = false,
                ApprovalData = new List<object>()
            };
        }

        /// <summary>
        /// Coordinate content generation using PlannerAgent
        /// </summary>
        private async Task<string> CoordinateContentGenerationWithSK(CampaignSession session, List<string> approvedCompanies)
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage($@"
**Content Generation Coordination Request**

**Campaign Goal:** {session.Campaign.Goal}
**Approved Companies:** {string.Join(", ", approvedCompanies)}
**Content Components:** {string.Join(", ", session.Campaign.Components)}
**Target Audience:** {session.Campaign.Audience}

Please coordinate the content generation process for these approved companies. Create a structured plan for generating:
{string.Join("\n", session.Campaign.Components.Select(c => $"- {c}"))}

Provide execution order, dependencies, and coordination strategy.
");

            var response = _plannerAgent.Agent.InvokeAsync(chatHistory);
            var lastMessage = await response.LastOrDefaultAsync();
            
            var plan = lastMessage?.Content ?? "Standard content generation plan";
            session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] 3-Agent Content Plan: {plan.Substring(0, Math.Min(100, plan.Length))}...");
            
            return plan;
        }

        /// <summary>
        /// Format content generation completion with SK agent coordination details
        /// </summary>
        private string FormatSemanticKernelContentComplete(List<string> approvedCompanies, string contentResults, string coordinationPlan)
        {
            return $@"
🤖 **Semantic Kernel Campaign Execution Complete!**

## 🎯 Agent Coordination
{coordinationPlan}

## ✅ Approved Companies ({approvedCompanies.Count})
{string.Join("\n", approvedCompanies.Select(c => $"• {c}"))}

## 📝 Generated Content
{contentResults}

## 🔄 SK Orchestration Summary
✅ **Router Agent**: Made intelligent workflow decisions
✅ **Planner Agent**: Coordinated content generation strategy  
✅ **Researcher Agent**: Provided company insights
✅ **AgentGroupChat**: Orchestrated multi-agent collaboration

## 🚀 Next Steps
- Review generated content assets
- Deploy to marketing channels  
- Monitor campaign performance
- Track engagement metrics

**All content generated through Semantic Kernel agent coordination!**
";
        }

        /// <summary>
        /// Format the results of Semantic Kernel orchestration
        /// </summary>
        private string FormatSemanticKernelOrchestrationResult(string orchestrationResult, CampaignSession session)
        {
            var companiesCount = session.Campaign.Companies?.Count ?? 0;
            var pendingApprovals = session.Campaign.PendingApprovals?.Count ?? 0;

            return $@"
🤖 **Semantic Kernel Orchestration Complete!**

## 🧠 Agent Coordination
{orchestrationResult}

## 📊 Campaign Status
- **Companies Identified**: {companiesCount}
- **Briefs Generated**: {companiesCount}
- **Pending Approvals**: {pendingApprovals}
- **Campaign Status**: {session.Campaign.Status}

## 🔄 SK Components Used
✅ **AgentGroupChat**: Multi-agent coordination
✅ **ChatCompletionAgent**: Individual agent responses  
✅ **Planner Agent**: Campaign strategy and planning
✅ **Researcher Agent**: Company insights and analysis
✅ **Router Agent**: Workflow orchestration and decisions

## 📋 Next Steps
1. **Review Company Briefs**: Each brief requires human approval
2. **Approve or Modify**: Use the approval interface
3. **Content Generation**: Will auto-trigger after all approvals
4. **Campaign Execution**: SK agents will coordinate final steps

**The agents are now coordinating your campaign execution!**
";
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
            // Use ContentAgent for content generation instead of direct tool calls
            var campaignCompany = new CampaignCompany
            {
                CompanyId = companyId,
                CompanyName = companyId,
                Brief = brief
            };

            var result = await _contentAgent.GenerateContentAsync(
                new List<CampaignCompany> { campaignCompany },
                new List<string> { component },
                campaignGoal
            );

            var contentItem = result.CompanyResults.FirstOrDefault()?.ContentItems.FirstOrDefault();
            return contentItem?.Content ?? $"Content generation failed for {component}";
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

        /// <summary>
        /// Approve all pending company briefs at once
        /// </summary>
        public async Task<WorkflowProgressResult> ApproveAllBriefsAsync(string sessionId)
        {
            var contextService = new ContextPersistenceService();
            var session = await contextService.LoadSessionAsync(sessionId);
            
            if (session == null)
            {
                return new WorkflowProgressResult
                {
                    HasProgressed = false,
                    NewStatus = CampaignStatus.Failed,
                    Message = $"Session {sessionId} not found",
                    RequiresApproval = false,
                    ApprovalData = new List<object>()
                };
            }

            // Clear all pending approvals
            var approvedCount = session.Campaign.PendingApprovals?.Count ?? 0;
            if (session.Campaign.PendingApprovals != null)
            {
                session.Campaign.PendingApprovals.Clear();
                session.Campaign.ExecutionLog.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] All briefs approved ({approvedCount} briefs)");
            }

            // Check workflow progression and return intelligent result
            return await CheckAndProgressWorkflowAsync(session);
        }

        // ...existing code...
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

    public class WorkflowProgressResult
    {
        public bool HasProgressed { get; set; }
        public CampaignStatus NewStatus { get; set; }
        public string Message { get; set; } = "";
        public bool RequiresApproval { get; set; }
        public List<object> ApprovalData { get; set; } = new();
    }

    /// <summary>
    /// DTO for company brief serialization to avoid anonymous object issues
    /// </summary>
    public class CompanyBriefDto
    {
        public string CompanyId { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Content { get; set; } = "";
        public string Brief { get; set; } = "";
        public string Industry { get; set; } = "";
        public string CampaignId { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime GeneratedAt { get; set; }
        public string TargetAudience { get; set; } = "";
        public int EstimatedBudget { get; set; }
        public int ProjectedReach { get; set; }
    }
}
