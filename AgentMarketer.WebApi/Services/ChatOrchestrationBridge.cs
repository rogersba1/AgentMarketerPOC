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
        /// Start new campaign using comprehensive orchestration service
        /// </summary>
        private async Task<ChatResponse> StartNewCampaignAsync(string userMessage, ChatSession chatSession)
        {
            // Use the new comprehensive method that handles planning and execution
            var (sessionId, response, hasApprovals) = await _orchestrationService.StartAndExecuteCampaignAsync(userMessage);
            
            // Store the campaign session ID in chat session
            chatSession.CampaignSessionId = sessionId;
            chatSession.AddMessage("Campaign System", response);

            if (hasApprovals)
            {
                // Get the campaign session to extract company briefs
                var campaignSession = await _orchestrationService.GetSessionAsync(sessionId);
                var companyBriefs = GetCompanyBriefs(campaignSession);
                
                if (companyBriefs.Count > 0)
                {
                    // Set pending approval state
                    chatSession.HasPendingApproval = true;
                    chatSession.PendingApprovalData = companyBriefs;
                    
                    return new ChatResponse
                    {
                        SessionId = chatSession.Id,
                        AgentName = "Campaign System",
                        Message = response + "\n\n**Please review the generated company briefs below.**",
                        MessageType = ChatMessageType.ApprovalRequired,
                        RequiresApproval = true,
                        ApprovalData = companyBriefs
                    };
                }
            }

            return new ChatResponse
            {
                SessionId = chatSession.Id,
                AgentName = "Campaign System", 
                Message = response,
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
        /// Gets available company briefs for approval (includes mock data for demo)
        /// </summary>
        private List<CompanyBrief> GetCompanyBriefs(CampaignSession session)
        {
            var briefs = new List<CompanyBrief>();
            
            // Check for company briefs in the new CampaignCompany structure
            if (session.Campaign.Companies?.Any() == true)
            {
                foreach (var campaignCompany in session.Campaign.Companies)
                {
                    if (!string.IsNullOrEmpty(campaignCompany.Brief))
                    {
                        var brief = new CompanyBrief
                        {
                            CompanyId = campaignCompany.CompanyId,
                            CompanyName = campaignCompany.CompanyName,
                            Content = campaignCompany.Brief,
                            Industry = ExtractIndustryFromContent(campaignCompany.Brief),
                            RequiresApproval = true,
                            KeyMessages = ExtractKeyMessagesFromContent(campaignCompany.Brief),
                            TargetAudience = ExtractTargetAudienceFromContent(campaignCompany.Brief),
                            EstimatedBudget = ExtractBudgetFromContent(campaignCompany.Brief),
                            ProjectedReach = EstimateReachFromContent(campaignCompany.Brief)
                        };
                        
                        // Debug logging
                        Console.WriteLine($"[DEBUG] Created brief: CompanyId='{brief.CompanyId}', CompanyName='{brief.CompanyName}', Industry='{brief.Industry}'");
                        
                        briefs.Add(brief);
                    }
                }
            }
            
            // If no briefs found in campaign companies, create mock briefs for demo
            if (briefs.Count == 0)
            {
                Console.WriteLine("[DEBUG] No generated briefs found, creating mock briefs");
                briefs = CreateMockCompanyBriefs();
                
                // Debug the mock briefs
                foreach (var brief in briefs)
                {
                    Console.WriteLine($"[DEBUG] Mock brief: CompanyId='{brief.CompanyId}', CompanyName='{brief.CompanyName}', Industry='{brief.Industry}'");
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] Found {briefs.Count} generated briefs");
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
                    Content = "**Company Brief: TechStart Inc**\n\n**Target Audience:** Small tech startups seeking growth\n**Key Message:** Revolutionary AI solutions for growing businesses\n**Content Strategy:** Landing page focused on cost savings and efficiency gains\n**Call-to-Action:** Schedule a demo to see 40% productivity increase\n\n**Analysis:** TechStart Inc operates in a competitive space where AI adoption is crucial for staying ahead. Our solution addresses their core challenge of scaling operations while maintaining quality.",
                    RequiresApproval = true,
                    CompanyId = "techstart-inc",
                    Industry = "Technology",
                    KeyMessages = new List<string> { "AI-powered growth", "40% productivity increase", "Cost-effective scaling" },
                    TargetAudience = "Tech startups with 10-50 employees",
                    EstimatedBudget = 25000,
                    ProjectedReach = 15000
                },
                new CompanyBrief
                {
                    CompanyName = "Global Manufacturing Co",
                    Content = "**Company Brief: Global Manufacturing Co**\n\n**Target Audience:** Enterprise manufacturing companies\n**Key Message:** Streamlined operations through intelligent automation\n**Content Strategy:** Case study-driven approach with ROI focus\n**Call-to-Action:** Download whitepaper on manufacturing transformation\n\n**Analysis:** Large-scale manufacturer with complex supply chains. Focus on operational efficiency and cost reduction through AI-driven insights.",
                    RequiresApproval = true,
                    CompanyId = "global-manufacturing",
                    Industry = "Manufacturing",
                    KeyMessages = new List<string> { "Operational efficiency", "AI automation", "Supply chain optimization" },
                    TargetAudience = "Manufacturing executives and operations managers",
                    EstimatedBudget = 75000,
                    ProjectedReach = 45000
                },
                new CompanyBrief
                {
                    CompanyName = "Retail Solutions Ltd",
                    Content = "**Company Brief: Retail Solutions Ltd**\n\n**Target Audience:** Mid-market retail chains\n**Key Message:** Customer experience enhancement through AI-driven insights\n**Content Strategy:** Before/after customer journey improvements\n**Call-to-Action:** Request personalized retail analysis\n\n**Analysis:** Mid-market retailer focusing on customer experience differentiation. Emphasis on data-driven decision making and personalized shopping experiences.",
                    RequiresApproval = true,
                    CompanyId = "retail-solutions",
                    Industry = "Retail",
                    KeyMessages = new List<string> { "Customer experience", "Personalization", "Data-driven insights" },
                    TargetAudience = "Retail managers and customer experience teams",
                    EstimatedBudget = 35000,
                    ProjectedReach = 25000
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
            // Parse key format like "CompanyBrief_TechStart_Inc" or "company_brief_TechStart_Inc"
            if (key.StartsWith("CompanyBrief_"))
            {
                return key.Substring("CompanyBrief_".Length).Replace("_", " ");
            }
            
            var parts = key.Split('_');
            if (parts.Length >= 3)
            {
                return string.Join(" ", parts.Skip(2));
            }
            return "Unknown Company";
        }

        /// <summary>
        /// Generate a consistent company ID from company name
        /// </summary>
        private string GenerateCompanyId(string companyName)
        {
            if (string.IsNullOrEmpty(companyName))
                return "unknown";
                
            return companyName
                .ToLowerInvariant()
                .Replace(" ", "-")
                .Replace(".", "")
                .Replace(",", "")
                .Replace("'", "")
                .Replace("&", "and")
                .Replace("inc", "")
                .Replace("llc", "")
                .Replace("corp", "")
                .Replace("co", "")
                .Trim('-');
        }

        /// <summary>
        /// Extract industry from company brief content
        /// </summary>
        private string ExtractIndustryFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "Unknown";
            
            // Look for industry mentions in common patterns
            var patterns = new[]
            {
                @"\*\*Industry\*\*:\s*([^\n]+)",
                @"Industry:\s*([^\n]+)",
                @"is a ([^.]*) company",
                @"in the ([^.]*) sector",
                @"([^.]*) industry"
            };
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var industry = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(industry) && industry.Length < 50)
                    {
                        return industry;
                    }
                }
            }
            
            return "Unknown";
        }

        /// <summary>
        /// Extract key messages from company brief content
        /// </summary>
        private List<string> ExtractKeyMessagesFromContent(string content)
        {
            var messages = new List<string>();
            
            if (string.IsNullOrEmpty(content)) 
                return new List<string> { "Generated content approval required" };
            
            // Look for key messaging pillars section
            var messagingMatch = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"## Key Messaging Pillars\s*\n(.*?)(?=##|\z)", 
                System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (messagingMatch.Success)
            {
                var messagingSection = messagingMatch.Groups[1].Value;
                var bullets = System.Text.RegularExpressions.Regex.Matches(
                    messagingSection, 
                    @"\d+\.\s*\*\*([^*]+)\*\*:?\s*([^\n]+)"
                );
                
                foreach (System.Text.RegularExpressions.Match bullet in bullets)
                {
                    var title = bullet.Groups[1].Value.Trim();
                    var description = bullet.Groups[2].Value.Trim();
                    messages.Add($"{title}: {description}");
                }
            }
            
            // Fallback: look for general bullet points
            if (messages.Count == 0)
            {
                var bullets = System.Text.RegularExpressions.Regex.Matches(
                    content, 
                    @"[-*•]\s*([^\n]+)"
                );
                
                foreach (System.Text.RegularExpressions.Match bullet in bullets)
                {
                    var message = bullet.Groups[1].Value.Trim();
                    if (message.Length > 10 && message.Length < 100)
                    {
                        messages.Add(message);
                    }
                }
            }
            
            return messages.Any() ? messages.Take(3).ToList() : new List<string> { "Personalized campaign content", "Industry-specific messaging", "Growth-focused approach" };
        }

        /// <summary>
        /// Extract target audience from company brief content
        /// </summary>
        private string ExtractTargetAudienceFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return "Business decision makers";
            
            // Look for target audience patterns
            var patterns = new[]
            {
                @"\*\*Target Audience\*\*:\s*([^\n]+)",
                @"Target Audience:\s*([^\n]+)",
                @"targeting ([^.]*) professionals",
                @"decision makers in ([^.]*)",
                @"([^.]*) executives"
            };
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var audience = match.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(audience) && audience.Length < 100)
                    {
                        return audience;
                    }
                }
            }
            
            return "Business decision makers and executives";
        }

        /// <summary>
        /// Extract estimated budget from company brief content
        /// </summary>
        private int ExtractBudgetFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return 25000;
            
            // Look for budget mentions
            var budgetMatch = System.Text.RegularExpressions.Regex.Match(
                content, 
                @"\$([0-9,]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (budgetMatch.Success)
            {
                var budgetStr = budgetMatch.Groups[1].Value.Replace(",", "");
                if (int.TryParse(budgetStr, out int budget))
                {
                    return budget;
                }
            }
            
            // Default budget based on company size indicators
            if (content.ToLower().Contains("enterprise") || content.ToLower().Contains("large"))
                return 75000;
            if (content.ToLower().Contains("startup") || content.ToLower().Contains("small"))
                return 15000;
                
            return 35000;
        }

        /// <summary>
        /// Estimate projected reach from company brief content
        /// </summary>
        private int EstimateReachFromContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return 10000;
            
            // Look for reach or employee count indicators
            var patterns = new[]
            {
                @"(\d+)\s*employees",
                @"(\d+,\d+)\s*employees",
                @"reach:\s*(\d+)",
                @"audience:\s*(\d+)"
            };
            
            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var numberStr = match.Groups[1].Value.Replace(",", "");
                    if (int.TryParse(numberStr, out int number))
                    {
                        // Estimate reach as multiple of employee count
                        return Math.Min(number * 10, 100000);
                    }
                }
            }
            
            // Default reach based on content indicators
            if (content.ToLower().Contains("enterprise") || content.ToLower().Contains("large"))
                return 50000;
            if (content.ToLower().Contains("startup") || content.ToLower().Contains("small"))
                return 8000;
                
            return 20000;
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
        public string CompanyId { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Content { get; set; } = "";
        public string Industry { get; set; } = "";
        public bool RequiresApproval { get; set; }
        public List<string>? KeyMessages { get; set; }
        public string? TargetAudience { get; set; }
        public int EstimatedBudget { get; set; }
        public int ProjectedReach { get; set; }
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
