using Microsoft.SemanticKernel;
using AgentOrchestration.Models;
using System.Threading.Tasks;

namespace AgentOrchestration.Agents
{
    /// <summary>
    /// Base interface for all agents in the orchestration system
    /// </summary>
    public interface IAgent
    {
        string Name { get; }
        string Description { get; }
        Task<string> ProcessAsync(string input, CampaignSession session);
    }

    /// <summary>
    /// Base implementation for agents using Semantic Kernel
    /// </summary>
    public abstract class BaseAgent : IAgent
    {
        protected readonly Kernel _kernel;
        protected readonly string _systemPrompt;

        public abstract string Name { get; }
        public abstract string Description { get; }

        protected BaseAgent(Kernel kernel, string systemPrompt)
        {
            _kernel = kernel;
            _systemPrompt = systemPrompt;
        }

        public abstract Task<string> ProcessAsync(string input, CampaignSession session);

        protected async Task<string> CallLLMAsync(string prompt, string userInput)
        {
            try
            {
                var fullPrompt = $"{_systemPrompt}\n\nUser Input: {userInput}\n\nResponse:";
                var response = await _kernel.InvokePromptAsync(fullPrompt);
                return response.ToString();
            }
            catch (Exception ex)
            {
                return $"Error processing request: {ex.Message}";
            }
        }
    }
}
