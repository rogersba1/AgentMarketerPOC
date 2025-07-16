using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using AgentOrchestration.Models;
using AgentOrchestration.Services;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents.Modern
{
    /// <summary>
    /// PlannerAgent: Parses user requests and determines execution requirements
    /// Responsibilities:
    /// - LLM-powered parsing of user requests
    /// - Extraction of target audience, company count, and content components
    /// - Creation of structured execution plans
    /// </summary>
    public class ModernPlannerAgent
    {
        private readonly CampaignRequestParsingService _parsingService;
        public ChatCompletionAgent Agent { get; private set; }

        public ModernPlannerAgent(Kernel kernel)
        {
            _parsingService = new CampaignRequestParsingService(kernel);
            Agent = CreateAgent(kernel);
        }

        private ChatCompletionAgent CreateAgent(Kernel kernel)
        {
            return new ChatCompletionAgent()
            {
                Instructions = @"
You are the Campaign Planner Agent responsible for parsing user requests and creating execution plans.

**Your Core Responsibilities:**
1. **Request Parsing**: Use LLM to intelligently parse user requests for:
   - Target Audience: retail, manufacturing, technology, healthcare, finance (default: retail)
   - Company Count: number of companies to target (default: 3, max: 10)
   - Content Components: landing_page, email, linkedin_post, ad_copy
   - Additional Context: special requirements or preferences

2. **Execution Planning**: Create structured plans that specify:
   - Research requirements for the target audience
   - Brief generation needs for identified companies
   - Content generation tasks for approved companies
   - Approval workflows and checkpoints

3. **Workflow Coordination**: Work with ResearchAgent and ContentAgent to:
   - Provide clear research directions based on parsed audience
   - Define content generation requirements based on parsed components
   - Ensure human-in-the-loop approval points are properly set up

**Input Processing:**
When you receive a user request, parse it to extract:
- What industry/audience they're targeting
- How many companies they want
- What marketing content they need
- Any special requirements

**Output Format:**
Always provide structured plans with clear next steps for Research and Content agents.

**Available Content Components:**
- landing_page: Personalized HTML landing pages
- email: Personalized email campaigns  
- linkedin_post: LinkedIn social media content
- ad_copy: Advertisement copy and creative

Remember: Your parsing sets the foundation for the entire campaign execution.
",
                Name = "PlannerAgent",
                Kernel = kernel
            };
        }

        /// <summary>
        /// Parse user request using LLM to extract structured campaign parameters
        /// </summary>
        public async Task<ParsedCampaignRequest> ParseUserRequestAsync(string userRequest)
        {
            return await _parsingService.ParseRequestAsync(userRequest);
        }

        /// <summary>
        /// Process planning request and return structured execution plan
        /// </summary>
        public async Task<string> CreateExecutionPlanAsync(string userRequest, ParsedCampaignRequest parsedRequest)
        {
            var planningPrompt = $@"
**User Request:** {userRequest}

**Parsed Parameters:**
- Target Audience: {parsedRequest.Audience}
- Company Count: {parsedRequest.CompanyCount}
- Content Components: {string.Join(", ", parsedRequest.Components)}
- Additional Context: {parsedRequest.AdditionalContext}
- Confidence Score: {parsedRequest.ConfidenceScore:F2}

Create a detailed execution plan that includes:
1. Research Phase requirements for {parsedRequest.Audience} industry
2. Brief generation plan for {parsedRequest.CompanyCount} companies
3. Content creation strategy for: {string.Join(", ", parsedRequest.Components)}
4. Approval workflow setup

Provide clear, actionable steps for the Research and Content agents.
";

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(planningPrompt);

            var response = Agent.InvokeAsync(chatHistory);
            var lastMessage = await response.LastOrDefaultAsync();
            
            return lastMessage?.Content ?? "Standard execution plan created";
        }
    }
}
