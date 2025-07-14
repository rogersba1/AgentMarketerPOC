using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using AgentOrchestration.Services;
using AgentOrchestration.Services.Modern;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentOrchestration.Services
{
    /// <summary>
    /// Integration bridge for Sequential Campaign Orchestration in existing CLI and Web API
    /// Simplified to use only SequentialCampaignOrchestrationService
    /// </summary>
    public class ModernOrchestrationBridge
    {
        private readonly SequentialCampaignOrchestrationService _sequentialService;
        private readonly ContextPersistenceService _persistenceService;

        public ModernOrchestrationBridge(Kernel kernel, ContextPersistenceService persistenceService)
        {
            _sequentialService = new SequentialCampaignOrchestrationService(kernel);
            _persistenceService = persistenceService;
        }

        /// <summary>
        /// Initialize the orchestration bridge
        /// </summary>
        public async Task InitializeAsync()
        {
            // Sequential service doesn't need initialization
            await Task.CompletedTask;
        }

        /// <summary>
        /// Execute campaign using sequential orchestration patterns (CLI version)
        /// </summary>
        public async Task<string> ExecuteModernCampaignAsync(string sessionId)
        {
            try
            {
                // Load session from persistence
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {
                    return "❌ Session not found. Please create a campaign first.";
                }

                if (session.Campaign == null)
                {
                    return "❌ No campaign found in session. Please create a campaign first.";
                }

                // Execute using sequential orchestration
                var result = await _sequentialService.ExecuteCampaignSequentiallyAsync(session, session.Campaign.Goal);

                // Save updated session
                await _persistenceService.SaveSessionAsync(session);

                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Modern orchestration failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Execute campaign from user prompt (for web API)
        /// </summary>
        public async Task<string> ExecuteCampaignAsync(string userPrompt, string? sessionId = null)
        {
            try
            {
                // Create or load session
                CampaignSession session;
                if (sessionId != null)
                {
                    var existingSession = await _persistenceService.LoadSessionAsync(sessionId);
                    session = existingSession ?? new CampaignSession { Id = sessionId };
                }
                else
                {
                    // Create new session
                    session = new CampaignSession();
                }

                // Create campaign if needed
                if (session.Campaign == null || string.IsNullOrEmpty(session.Campaign.Name))
                {
                    session.Campaign = new Campaign
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Sequential Campaign",
                        Goal = userPrompt,
                        CreatedAt = DateTime.UtcNow,
                        Status = CampaignStatus.Draft
                    };
                }

                // Execute using sequential orchestration
                await _sequentialService.ExecuteCampaignSequentiallyAsync(session, userPrompt);

                // Save updated session
                await _persistenceService.SaveSessionAsync(session);

                return session.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"Modern orchestration failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute collaborative planning session (CLI version)
        /// </summary>
        public async Task<string> ExecuteCollaborativePlanningAsync(string sessionId, string userInput)
        {
            try
            {
                // Load session from persistence
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {
                    return "❌ Session not found. Please create a campaign first.";
                }

                if (session.Campaign == null)
                {
                    return "❌ No campaign found in session. Please create a campaign first.";
                }

                // Execute sequential orchestration (replaces collaborative planning)
                var result = await _sequentialService.ExecuteCampaignSequentiallyAsync(session, userInput);

                // Save updated session
                await _persistenceService.SaveSessionAsync(session);

                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Sequential orchestration failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Execute collaborative planning from user prompt (for web API)
        /// </summary>
        public async Task<string> ExecuteCollaborativePlanningWebAsync(string userPrompt, string? sessionId = null)
        {
            try
            {
                // Create or load session
                CampaignSession session;
                if (sessionId != null)
                {
                    var existingSession = await _persistenceService.LoadSessionAsync(sessionId);
                    session = existingSession ?? new CampaignSession { Id = sessionId };
                }
                else
                {
                    // Create new session
                    session = new CampaignSession();
                }

                // Create campaign if needed
                if (session.Campaign == null || string.IsNullOrEmpty(session.Campaign.Name))
                {
                    session.Campaign = new Campaign
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Sequential Planning Session",
                        Goal = userPrompt,
                        CreatedAt = DateTime.UtcNow,
                        Status = CampaignStatus.Draft
                    };
                }

                // Execute sequential orchestration
                await _sequentialService.ExecuteCampaignSequentiallyAsync(session, userPrompt);

                // Save updated session
                await _persistenceService.SaveSessionAsync(session);

                return session.Id;
            }
            catch (Exception ex)
            {
                throw new Exception($"Sequential orchestration failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Execute campaign using Sequential Orchestration pattern (unified approach)
        /// Company Discovery → Brief Generation → Human Approval → Content Generation
        /// </summary>
        public async Task<string> ExecuteSequentialCampaignAsync(string sessionId, string userRequest)
        {
            try
            {
                // Load session from persistence
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {
                    return "❌ Session not found. Please create a campaign first.";
                }

                if (session.Campaign == null)
                {
                    return "❌ No campaign found in session. Please create a campaign first.";
                }

                // Execute Sequential Orchestration workflow
                var result = await _sequentialService.ExecuteCampaignSequentiallyAsync(session, userRequest);
                
                // Save updated session
                await _persistenceService.SaveSessionAsync(session);

                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Sequential campaign execution failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Get orchestration status for web API
        /// </summary>
        public async Task<OrchestrationStatusInfo> GetOrchestrationStatusAsync(string sessionId)
        {
            try
            {
                var session = await _persistenceService.LoadSessionAsync(sessionId);
                if (session == null)
                {                return new OrchestrationStatusInfo
                {
                    SessionId = sessionId,
                    IsActive = false,
                    CurrentStage = "Session not found",
                    CompletedStages = new List<string>(),
                    Messages = new List<string> { "Session not found" },
                    LastActivity = DateTime.MinValue
                };
                }

                var lastActivity = session.LastUpdated != DateTime.MinValue 
                    ? session.LastUpdated 
                    : session.Campaign?.CreatedAt ?? DateTime.MinValue;

                return new OrchestrationStatusInfo
                {
                    SessionId = sessionId,
                    IsActive = session.Campaign?.Status == CampaignStatus.InProgress,
                    CurrentStage = session.Campaign?.Status.ToString() ?? "Unknown",
                    CompletedStages = session.Campaign?.ExecutionLog ?? new List<string>(),
                    Messages = session.Campaign?.ExecutionLog ?? new List<string>(),
                    LastActivity = lastActivity
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get orchestration status: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get active sessions for web API
        /// </summary>
        public async Task<List<OrchestrationStatusInfo>> GetActiveSessionsAsync()
        {
            try
            {
                var activeSessions = await _persistenceService.GetActiveSessionsAsync();
                var result = new List<OrchestrationStatusInfo>();

                foreach (var sessionInfo in activeSessions)
                {
                    var status = await GetOrchestrationStatusAsync(sessionInfo.Id);
                    status.SessionId = sessionInfo.Id; // Ensure SessionId is set
                    result.Add(status);
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get active sessions: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get modern orchestration status and capabilities (CLI version)
        /// </summary>
        public string GetModernOrchestrationStatus()
        {
            return @"
🚀 **Modern Semantic Kernel Orchestration Available!**

**Enhanced Capabilities:**
✅ ChatCompletionAgent architecture
✅ Sequential agent coordination
✅ Multi-turn collaborative planning
✅ Context-aware processing
✅ Enhanced error handling
✅ Optimized performance patterns

**Available Commands:**
- **`execute-modern`** - Run campaign with modern orchestration
- **`collaborate <input>`** - Multi-agent collaborative planning

**Key Benefits:**
- Better agent coordination than custom router
- Built-in context management
- More robust error handling
- Future-ready for advanced SK features

**Status:** ✅ Ready for use
**SK Version:** 1.40.0-preview
";
        }

        /// <summary>
        /// Cleanup orchestration resources
        /// </summary>
        public async Task CleanupAsync()
        {
            // Sequential service doesn't need cleanup
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Orchestration status information for web API
    /// </summary>
    public class OrchestrationStatusInfo
    {
        public string SessionId { get; set; } = "";
        public bool IsActive { get; set; }
        public string CurrentStage { get; set; } = "";
        public List<string> CompletedStages { get; set; } = new();
        public List<string> Messages { get; set; } = new();
        public DateTime LastActivity { get; set; }
    }
}
