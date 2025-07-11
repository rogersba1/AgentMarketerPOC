using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using AgentOrchestration.Services;
using AgentOrchestration.Services.Modern;
using System;
using System.Threading.Tasks;

namespace AgentOrchestration.Services
{
    /// <summary>
    /// Integration bridge for modern orchestration in existing CLI
    /// </summary>
    public class ModernOrchestrationBridge
    {
        private readonly ModernOrchestrationService _modernService;
        private readonly ContextPersistenceService _persistenceService;

        public ModernOrchestrationBridge(Kernel kernel, ContextPersistenceService persistenceService)
        {
            _modernService = new ModernOrchestrationService(kernel);
            _persistenceService = persistenceService;
        }

        /// <summary>
        /// Initialize the modern orchestration bridge
        /// </summary>
        public async Task InitializeAsync()
        {
            await _modernService.InitializeAsync();
        }

        /// <summary>
        /// Execute campaign using modern orchestration patterns
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

                // Execute using modern orchestration
                var result = await _modernService.ExecuteCampaignAsync(session);

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
        /// Execute collaborative planning session
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

                // Execute collaborative planning
                var result = await _modernService.ExecuteCollaborativePlanningAsync(session, userInput);

                // Save updated session
                await _persistenceService.SaveSessionAsync(session);

                return result;
            }
            catch (Exception ex)
            {
                return $"❌ Collaborative planning failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Get modern orchestration status and capabilities
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
        /// Cleanup modern orchestration resources
        /// </summary>
        public async Task CleanupAsync()
        {
            await _modernService.CleanupAsync();
        }
    }
}
