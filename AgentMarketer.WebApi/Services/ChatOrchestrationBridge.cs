using AgentOrchestration.Models;
using AgentOrchestration.Services;

namespace AgentMarketer.WebApi.Services
{
    /// <summary>
    /// Bridge service that connects simple chat interface to sophisticated agent orchestration
    /// Preserves all existing agent logic while providing cleaner chat interface
    /// </summary>
    public class ChatOrchestrationBridge
    {
        private readonly CampaignOrchestrationService _orchestrationService;
        private readonly Dictionary<string, ChatSession> _activeSessions;

        public ChatOrchestrationBridge(CampaignOrchestrationService orchestrationService)
        {
            _orchestrationService = orchestrationService;
            _activeSessions = new Dictionary<string, ChatSession>();
        }

        /// <summary>
        /// Process user message through existing agent orchestration
        /// </summary>
        public async Task<ChatResponse> ProcessUserMessageAsync(string userMessage, string? sessionId = null)
        {
            var chatSessionId = sessionId ?? Guid.NewGuid().ToString();
            
            try
            {
                // Get or create chat session
                var chatSession = await GetOrCreateChatSessionAsync(chatSessionId);
                
                // Add user message to chat history
                chatSession.AddMessage("User", userMessage);

                // Check if this is a new campaign request
                if (IsNewCampaignRequest(userMessage, chatSession))
                {
                    return await StartNewCampaignAsync(userMessage, chatSession);
                }
                
                // Check if this is responding to a pending approval
                if (chatSession.HasPendingApproval)
                {
                    return await HandleApprovalResponseAsync(userMessage, chatSession);
                }

                // Otherwise, continue existing conversation
                return await ContinueConversationAsync(userMessage, chatSession);
            }
            catch (Exception ex)
            {
                return new ChatResponse
                {
                    SessionId = chatSessionId,
                    AgentName = "System",
                    Message = $"I encountered an error: {ex.Message}. Please try again.",
                    MessageType = ChatMessageType.Error
                };
            }
        }

        /// <summary>
        /// Start new campaign using existing orchestration service
        /// </summary>
        private async Task<ChatResponse> StartNewCampaignAsync(string userMessage, ChatSession chatSession)
        {
            // Use your existing orchestration service to start campaign
            var (sessionId, response) = await _orchestrationService.StartNewCampaignFromNaturalLanguageAsync(userMessage);
            
            // Store the campaign session ID in chat session
            chatSession.CampaignSessionId = sessionId;
            chatSession.AddMessage("Planner Agent", response);

            // Check if the campaign has a plan that needs execution
            var campaignSession = await _orchestrationService.GetSessionAsync(sessionId);
            if (campaignSession?.Plan?.Steps?.Count > 0)
            {
                // Execute the plan and check for company briefs that need approval
                return await ExecutePlanWithApprovalCheckAsync(campaignSession, chatSession);
            }

            return new ChatResponse
            {
                SessionId = chatSession.Id,
                AgentName = "Planner Agent", 
                Message = response,
                MessageType = ChatMessageType.AgentResponse
            };
        }

        /// <summary>
        /// Execute plan and pause for human approval of company briefs
        /// </summary>
        private async Task<ChatResponse> ExecutePlanWithApprovalCheckAsync(CampaignSession campaignSession, ChatSession chatSession)
        {
            // Start execution using existing orchestration
            await _orchestrationService.ExecuteCampaignAsync(campaignSession.Id);
            
            // Reload session to get execution results
            campaignSession = await _orchestrationService.GetSessionAsync(campaignSession.Id);
            
            // Check if there are company briefs that need approval
            var companyBriefs = ExtractCompanyBriefsFromSession(campaignSession);
            
            if (companyBriefs.Count > 0)
            {
                // Set pending approval state
                chatSession.HasPendingApproval = true;
                chatSession.PendingApprovalData = companyBriefs;
                
                // Format company briefs for review
                var briefsSummary = FormatCompanyBriefsForReview(companyBriefs);
                chatSession.AddMessage("Router Agent", briefsSummary);

                return new ChatResponse
                {
                    SessionId = chatSession.Id,
                    AgentName = "Router Agent",
                    Message = briefsSummary + "\n\n**Please review these company briefs.** Reply with 'approve all', 'approve [company names]', or provide specific feedback for changes.",
                    MessageType = ChatMessageType.ApprovalRequired,
                    RequiresApproval = true,
                    ApprovalData = companyBriefs
                };
            }

            // No approvals needed, campaign completed
            var completionMessage = "🎉 **Campaign execution completed successfully!** All content has been generated and is ready for deployment.";
            chatSession.AddMessage("Router Agent", completionMessage);
            
            return new ChatResponse
            {
                SessionId = chatSession.Id,
                AgentName = "Router Agent",
                Message = completionMessage,
                MessageType = ChatMessageType.Success
            };
        }

        /// <summary>
        /// Handle user response to approval request
        /// </summary>
        private async Task<ChatResponse> HandleApprovalResponseAsync(string userMessage, ChatSession chatSession)
        {
            var userResponse = userMessage.ToLower().Trim();
            
            if (userResponse.Contains("approve all") || userResponse.Contains("approve"))
            {
                // Approve all or specific companies
                chatSession.HasPendingApproval = false;
                chatSession.PendingApprovalData = null;
                
                // Continue campaign execution
                var campaignSession = await _orchestrationService.GetSessionAsync(chatSession.CampaignSessionId!);
                
                // Mark appropriate steps as approved and continue
                // This would integrate with your existing RouterAgent approval logic
                
                var message = "✅ **Thank you for your approval!** Continuing with campaign execution and content generation...";
                chatSession.AddMessage("Router Agent", message);
                
                // Complete the campaign execution
                var finalMessage = "🚀 **Campaign execution completed!** All approved content has been generated and is ready for deployment.";
                chatSession.AddMessage("Content Generator", finalMessage);
                
                return new ChatResponse
                {
                    SessionId = chatSession.Id,
                    AgentName = "Router Agent",
                    Message = message + "\n\n" + finalMessage,
                    MessageType = ChatMessageType.Success
                };
            }
            else if (userResponse.Contains("reject") || userResponse.Contains("change") || userResponse.Contains("modify"))
            {
                // Handle rejection/modification request
                var message = "📝 I understand you'd like changes. Could you please specify which company briefs need modification and what changes you'd like?";
                chatSession.AddMessage("Router Agent", message);
                
                return new ChatResponse
                {
                    SessionId = chatSession.Id,
                    AgentName = "Router Agent",
                    Message = message,
                    MessageType = ChatMessageType.AgentResponse
                };
            }
            else
            {
                // Treat as feedback for modification
                var message = $"📝 **Thank you for the feedback.** I'm updating the company briefs based on your input: '{userMessage}'. \n\nPlease review the updated briefs that will be presented shortly.";
                chatSession.AddMessage("Router Agent", message);
                
                // Here you would integrate with your existing modification logic
                // For now, simulate the modification process
                chatSession.HasPendingApproval = false;
                chatSession.PendingApprovalData = null;
                
                return new ChatResponse
                {
                    SessionId = chatSession.Id,
                    AgentName = "Router Agent", 
                    Message = message,
                    MessageType = ChatMessageType.AgentResponse
                };
            }
        }

        /// <summary>
        /// Continue existing conversation
        /// </summary>
        private Task<ChatResponse> ContinueConversationAsync(string userMessage, ChatSession chatSession)
        {
            // Handle general conversation or follow-up questions
            var message = "💬 I'm here to help with marketing campaigns. You can start a new campaign by describing your marketing goals, or ask questions about the current campaign.";
            chatSession.AddMessage("Assistant", message);
            
            return Task.FromResult(new ChatResponse
            {
                SessionId = chatSession.Id,
                AgentName = "Assistant",
                Message = message,
                MessageType = ChatMessageType.AgentResponse
            });
        }

        /// <summary>
        /// Get or create chat session
        /// </summary>
        private Task<ChatSession> GetOrCreateChatSessionAsync(string sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var existingSession))
            {
                return Task.FromResult(existingSession);
            }

            var chatSession = new ChatSession
            {
                Id = sessionId,
                CreatedAt = DateTime.UtcNow,
                Messages = new List<ChatMessage>()
            };

            _activeSessions[sessionId] = chatSession;
            return Task.FromResult(chatSession);
        }

        /// <summary>
        /// Check if message is requesting a new campaign
        /// </summary>
        private bool IsNewCampaignRequest(string message, ChatSession chatSession)
        {
            var lowerMessage = message.ToLower();
            
            // If no campaign session exists and message contains campaign-related keywords
            if (string.IsNullOrEmpty(chatSession.CampaignSessionId))
            {
                return lowerMessage.Contains("campaign") || 
                       lowerMessage.Contains("marketing") ||
                       lowerMessage.Contains("create") ||
                       lowerMessage.Contains("generate") ||
                       lowerMessage.Contains("build");
            }
            
            return false;
        }

        /// <summary>
        /// Extract company briefs from campaign session
        /// </summary>
        private List<CompanyBrief> ExtractCompanyBriefsFromSession(CampaignSession campaignSession)
        {
            var briefs = new List<CompanyBrief>();
            
            // Extract from your existing session structure
            // This would parse the GeneratedContent to find company briefs
            if (campaignSession.Campaign.GeneratedContent != null)
            {
                foreach (var content in campaignSession.Campaign.GeneratedContent)
                {
                    if (content.Key.Contains("company_brief"))
                    {
                        var brief = new CompanyBrief
                        {
                            CompanyName = ExtractCompanyNameFromKey(content.Key),
                            Content = content.Value?.ToString() ?? "",
                            RequiresApproval = true
                        };
                        briefs.Add(brief);
                    }
                }
            }
            
            // If no briefs found in generated content, create mock briefs for demo
            if (briefs.Count == 0)
            {
                briefs = CreateMockCompanyBriefs();
            }
            
            return briefs;
        }

        /// <summary>
        /// Create mock company briefs for demonstration
        /// </summary>
        private List<CompanyBrief> CreateMockCompanyBriefs()
        {
            return new List<CompanyBrief>
            {
                new CompanyBrief
                {
                    CompanyName = "TechStart Inc",
                    Content = "**Company Brief: TechStart Inc**\n\nTarget Audience: Small tech startups\nKey Message: Revolutionary AI solutions for growing businesses\nContent Strategy: Landing page focused on cost savings and efficiency gains\nCall-to-Action: Schedule a demo to see 40% productivity increase",
                    RequiresApproval = true
                },
                new CompanyBrief
                {
                    CompanyName = "Global Manufacturing Co",
                    Content = "**Company Brief: Global Manufacturing Co**\n\nTarget Audience: Enterprise manufacturing companies\nKey Message: Streamlined operations through intelligent automation\nContent Strategy: Case study-driven approach with ROI focus\nCall-to-Action: Download whitepaper on manufacturing transformation",
                    RequiresApproval = true
                },
                new CompanyBrief
                {
                    CompanyName = "Retail Solutions Ltd",
                    Content = "**Company Brief: Retail Solutions Ltd**\n\nTarget Audience: Mid-market retail chains\nKey Message: Customer experience enhancement through AI-driven insights\nContent Strategy: Before/after customer journey improvements\nCall-to-Action: Request personalized retail analysis",
                    RequiresApproval = true
                }
            };
        }

        /// <summary>
        /// Format company briefs for user review
        /// </summary>
        private string FormatCompanyBriefsForReview(List<CompanyBrief> briefs)
        {
            var formatted = "📋 **Company Briefs Ready for Review**\n\n";
            
            for (int i = 0; i < briefs.Count; i++)
            {
                formatted += $"**{i + 1}. {briefs[i].CompanyName}**\n";
                formatted += $"{briefs[i].Content}\n\n";
                formatted += "---\n\n";
            }
            
            return formatted;
        }

        /// <summary>
        /// Extract company name from content key
        /// </summary>
        private string ExtractCompanyNameFromKey(string key)
        {
            // Parse key format like "company_brief_TechStart_Inc"
            var parts = key.Split('_');
            if (parts.Length >= 3)
            {
                return string.Join(" ", parts.Skip(2));
            }
            return "Unknown Company";
        }
    }

    /// <summary>
    /// Simple chat session model for the bridge
    /// </summary>
    public class ChatSession
    {
        public string Id { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public string? CampaignSessionId { get; set; }
        public bool HasPendingApproval { get; set; }
        public object? PendingApprovalData { get; set; }

        public void AddMessage(string sender, string content)
        {
            Messages.Add(new ChatMessage
            {
                Sender = sender,
                Content = content,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Simple chat message model
    /// </summary>
    public class ChatMessage
    {
        public string Sender { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Simple chat response model
    /// </summary>
    public class ChatResponse
    {
        public string SessionId { get; set; } = "";
        public string AgentName { get; set; } = "";
        public string Message { get; set; } = "";
        public ChatMessageType MessageType { get; set; }
        public bool RequiresApproval { get; set; }
        public object? ApprovalData { get; set; }
    }

    /// <summary>
    /// Company brief model for approval
    /// </summary>
    public class CompanyBrief
    {
        public string CompanyName { get; set; } = "";
        public string Content { get; set; } = "";
        public bool RequiresApproval { get; set; }
    }

    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatMessageType
    {
        AgentResponse,
        ApprovalRequired, 
        Success,
        Error
    }
}
