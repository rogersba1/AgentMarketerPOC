# GitHub Copilot Instructions for Multi-Agent Campaign Orchestration System

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Overview
This is a proof-of-concept multi-agent orchestration system for marketing campaign creation and execution. The system demonstrates agentic patterns using Microsoft Semantic Kernel and Azure OpenAI.

## Architecture
- **Planner Agent**: Creates structured campaign execution plans
- **Researcher Agent**: Provides customer insights and audience analysis
- **Router Agent**: Orchestrates plan execution across agents and tools
- **Content Generation Tools**: Stub implementations for various marketing content types
- **Context Persistence Service**: Manages long-running conversation state

## Key Technologies
- Microsoft Semantic Kernel for AI orchestration
- Azure OpenAI for language model capabilities
- .NET 8 for application framework
- JSON for data serialization and persistence

## Development Guidelines
1. Follow the existing agent pattern when adding new agents
2. Implement proper error handling and logging
3. Use async/await patterns consistently
4. Maintain session state through the ContextPersistenceService
5. Add appropriate documentation for new functionality

## Agent Development Pattern
When creating new agents:
1. Inherit from BaseAgent
2. Implement the ProcessAsync method
3. Define a clear system prompt
4. Handle errors gracefully
5. Log activities to the campaign execution log

## Testing Notes
- The system uses mock data for customer insights
- Content generation tools return placeholder content
- Approval processes are simulated for the prototype
- Session persistence uses local file storage

## Azure Integration
- Configured for Azure OpenAI with fallback to OpenAI
- Uses managed identity patterns where applicable
- Follows Azure SDK best practices for authentication
