using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace AgentMarketer.WebApi.Services
{
    /// <summary>
    /// Mock chat completion service for testing when no AI service is configured
    /// </summary>
    public class MockChatCompletionService : IChatCompletionService
    {
        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>();

        public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
            ChatHistory chatHistory, 
            PromptExecutionSettings? executionSettings = null, 
            Kernel? kernel = null, 
            CancellationToken cancellationToken = default)
        {
            // Simulate some processing time
            await Task.Delay(100, cancellationToken);

            // Create a mock response based on the last user message
            var lastMessage = chatHistory.LastOrDefault()?.Content ?? "No message provided";
            var mockResponse = GenerateMockResponse(lastMessage);

            return new List<ChatMessageContent>
            {
                new ChatMessageContent(AuthorRole.Assistant, mockResponse)
            };
        }

        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
            ChatHistory chatHistory, 
            PromptExecutionSettings? executionSettings = null, 
            Kernel? kernel = null, 
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var response = await GetChatMessageContentsAsync(chatHistory, executionSettings, kernel, cancellationToken);
            foreach (var content in response)
            {
                yield return new StreamingChatMessageContent(AuthorRole.Assistant, content.Content);
            }
        }

        private string GenerateMockResponse(string input)
        {
            var sb = new StringBuilder();
            
            if (input.ToLower().Contains("campaign"))
            {
                sb.AppendLine("🎯 **Mock Campaign Analysis**");
                sb.AppendLine();
                sb.AppendLine("Based on your request, I've analyzed the campaign requirements:");
                sb.AppendLine();
                sb.AppendLine("**Target Companies:**");
                sb.AppendLine("- Company A: Retail leader with strong digital presence");
                sb.AppendLine("- Company B: Emerging retail brand focused on sustainability");
                sb.AppendLine("- Company C: Traditional retailer expanding online");
                sb.AppendLine();
                sb.AppendLine("**Recommended Content:**");
                sb.AppendLine("- Landing pages with personalized messaging");
                sb.AppendLine("- Email campaigns targeting decision makers");
                sb.AppendLine("- Social media content highlighting value propositions");
                sb.AppendLine();
                sb.AppendLine("**Next Steps:**");
                sb.AppendLine("1. Research target company specifics");
                sb.AppendLine("2. Develop personalized content strategy");
                sb.AppendLine("3. Create content assets");
                sb.AppendLine("4. Launch campaign with tracking");
                sb.AppendLine();
                sb.AppendLine("*Note: This is a mock response. Configure Azure OpenAI or OpenAI API keys for full functionality.*");
            }
            else if (input.ToLower().Contains("planning"))
            {
                sb.AppendLine("🤝 **Mock Collaborative Planning Session**");
                sb.AppendLine();
                sb.AppendLine("**Research Agent Findings:**");
                sb.AppendLine("- Market analysis completed");
                sb.AppendLine("- Competitor research identified key differentiators");
                sb.AppendLine("- Target audience personas defined");
                sb.AppendLine();
                sb.AppendLine("**Planner Agent Strategy:**");
                sb.AppendLine("- Multi-channel approach recommended");
                sb.AppendLine("- Content calendar developed");
                sb.AppendLine("- Performance metrics defined");
                sb.AppendLine();
                sb.AppendLine("**Collaborative Recommendations:**");
                sb.AppendLine("- Cross-functional team alignment needed");
                sb.AppendLine("- Budget allocation optimized");
                sb.AppendLine("- Timeline established with key milestones");
                sb.AppendLine();
                sb.AppendLine("*Note: This is a mock collaborative planning response. Configure AI services for actual agent collaboration.*");
            }
            else
            {
                sb.AppendLine("🤖 **Mock Response**");
                sb.AppendLine();
                sb.AppendLine($"I understand you're asking about: \"{input}\"");
                sb.AppendLine();
                sb.AppendLine("This is a mock response from the testing chat completion service.");
                sb.AppendLine("To get actual AI-powered responses, please configure either:");
                sb.AppendLine("- Azure OpenAI API keys in appsettings.json");
                sb.AppendLine("- OpenAI API keys in appsettings.json");
                sb.AppendLine();
                sb.AppendLine("The modern orchestration system is ready and will work with real AI services once configured.");
            }

            return sb.ToString();
        }
    }
}
