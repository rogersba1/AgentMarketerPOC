using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using AgentOrchestration.Models;
using System.Collections.Generic;

namespace AgentOrchestration.Agents.Modern
{
    /// <summary>
    /// Modern agent factory using latest Semantic Kernel ChatCompletionAgent
    /// </summary>
    public static class ModernAgentFactory
    {
        /// <summary>
        /// Creates a modern ChatCompletionAgent with proper configuration
        /// </summary>
        public static ChatCompletionAgent CreateAgent(
            string name,
            string instructions,
            Kernel kernel,
            IEnumerable<KernelPlugin>? plugins = null)
        {
            // Clone kernel for agent-specific configuration
            var agentKernel = kernel.Clone();

            // Add plugins if provided
            if (plugins != null)
            {
                foreach (var plugin in plugins)
                {
                    agentKernel.Plugins.Add(plugin);
                }
            }

            // Create agent with modern SK configuration
            return new ChatCompletionAgent()
            {
                Name = name,
                Instructions = instructions,
                Kernel = agentKernel,
                Arguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings()
                    {
                        FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                        Temperature = 0.7,
                        MaxTokens = 2000
                    })
            };
        }

        /// <summary>
        /// Creates a modern agent with custom execution settings
        /// </summary>
        public static ChatCompletionAgent CreateAgentWithSettings(
            string name,
            string instructions,
            Kernel kernel,
            OpenAIPromptExecutionSettings settings,
            IEnumerable<KernelPlugin>? plugins = null)
        {
            var agentKernel = kernel.Clone();

            if (plugins != null)
            {
                foreach (var plugin in plugins)
                {
                    agentKernel.Plugins.Add(plugin);
                }
            }

            return new ChatCompletionAgent()
            {
                Name = name,
                Instructions = instructions,
                Kernel = agentKernel,
                Arguments = new KernelArguments(settings)
            };
        }
    }

    /// <summary>
    /// Interface for modern agents that can be used with orchestration
    /// </summary>
    public interface IModernAgent
    {
        string Name { get; }
        string Description { get; }
        ChatCompletionAgent Agent { get; }
        Task<string> ProcessAsync(string input, CampaignSession session);
    }

    /// <summary>
    /// Base class for modern agents that bridges old interface with new ChatCompletionAgent
    /// </summary>
    public abstract class ModernAgentBase : IAgent, IModernAgent
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public ChatCompletionAgent Agent { get; private set; }

        protected ModernAgentBase(ChatCompletionAgent agent)
        {
            Agent = agent;
        }

        public abstract Task<string> ProcessAsync(string input, CampaignSession session);

        /// <summary>
        /// Helper method to invoke the modern agent with proper chat context
        /// </summary>
        protected async Task<string> InvokeAgentAsync(string input, CampaignSession? session = null)
        {
            try
            {
                // Create chat history from session context if available
                var chatHistory = new ChatHistory();
                
                if (session != null && !string.IsNullOrEmpty(session.CurrentContext))
                {
                    chatHistory.AddSystemMessage(session.CurrentContext);
                }

                chatHistory.AddUserMessage(input);

                // Invoke the modern agent
                var response = Agent.InvokeAsync(chatHistory);
                var lastMessage = await response.LastAsync();
                return lastMessage.Content ?? "No response generated";
            }
            catch (Exception ex)
            {
                return $"Error processing request: {ex.Message}";
            }
        }
    }
}
