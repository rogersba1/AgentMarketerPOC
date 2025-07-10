# Multi-Agent Campaign Orchestration System

A proof-of-concept demonstration of multi-agent orchestration for marketing campaign creation and execution using Microsoft Semantic Kernel and Azure OpenAI.

## Overview

This system demonstrates agentic patterns for marketing campaign automation, enabling marketers to:
- Initiate campaigns by selecting audiences
- Create campaign briefs
- Select campaign components
- Review and approve generated content
- Execute campaigns through an orchestrated workflow

## Architecture

### Project Separation
The solution is architected with a clear separation of concerns:

- **AgentOrchestration**: Core library containing all the agent logic, models, services, and tools. This library is framework-agnostic and can be used by any .NET application.
- **AgentCmdClient**: Command-line interface that demonstrates the capabilities of the AgentOrchestration library. This provides an interactive way to test and explore the system.

This separation allows for:
- **Reusability**: The core library can be integrated into web applications, APIs, or other client types
- **Testability**: Core logic can be unit tested independently of the UI
- **Maintainability**: Clear boundaries between business logic and presentation layer
- **Extensibility**: Easy to add new client types (web, API, desktop) without modifying core logic

### Core Agents

### Core Agents
- **Planner Agent**: Creates structured campaign execution plans based on user goals
- **Researcher Agent**: Provides customer insights and audience analysis using mock data
- **Router Agent**: Orchestrates plan execution across multiple agents and tools

### Supporting Components
- **Content Generation Tools**: Stub implementations for landing pages, emails, LinkedIn posts, and ads
- **Context Persistence Service**: Manages long-running conversation state
- **Campaign Orchestration Service**: Main service coordinating all components

## Features

### WBS Implementation Status
- ✅ **Environment Setup**: .NET 9 project with Semantic Kernel and Azure OpenAI integration
- ✅ **Planner Agent**: Creates structured campaign plans with sequential steps
- ✅ **Researcher Agent**: Provides customer insights from mock data
- ✅ **Content Generation Tools**: Stub implementations for all major content types
- ✅ **Router Agent**: Orchestrates plan execution with human-in-the-loop simulation
- ✅ **Context Persistence**: Long-running conversation state management
- ✅ **Integration Testing**: Full end-to-end workflow demonstration

### Key Capabilities
- **Campaign Planning**: AI-driven creation of structured execution plans
- **Audience Research**: Mock customer data analysis and insights generation
- **Content Generation**: Placeholder content for various marketing channels
- **Workflow Orchestration**: Sequential execution with approval gates
- **Session Management**: Persistent state across conversation sessions
- **Human-in-the-Loop**: Simulated approval processes for content review

## Getting Started

### Prerequisites
- .NET 9 SDK
- Azure OpenAI access (optional - system works with basic functionality without it)
- Visual Studio Code or Visual Studio 2022

### Configuration
1. Configure your Azure OpenAI or OpenAI API key in the AgentCmdClient project:
   ```bash
   cd AgentCmdClient
   dotnet user-secrets set "AzureOpenAI:ApiKey" "your-api-key"
   dotnet user-secrets set "AzureOpenAI:Endpoint" "your-endpoint"
   ```

2. Or update `AgentCmdClient/appsettings.json` with your configuration (not recommended for production)

### Running the Application
```bash
# From the solution root directory
dotnet run --project AgentCmdClient

# Or navigate to the client directory
cd AgentCmdClient
dotnet run
```

## Building and Testing

### Build the Solution
```bash
# Build both projects
dotnet build

# Build specific project
dotnet build AgentOrchestration
dotnet build AgentCmdClient
```

### Run Tests
```bash
# Run all tests (when test projects are added)
dotnet test

# Test specific project
dotnet test AgentOrchestration.Tests
```

### Development Workflow
1. Make changes to the core library (`AgentOrchestration`)
2. Test changes using the command-line client (`AgentCmdClient`)
3. Build and verify compilation
4. Run the interactive demo to test functionality
```

## Usage Examples

### Quick Start (Option 10 - Full Demo)
The application includes a complete end-to-end demo that demonstrates:
1. Creating a new campaign
2. Generating an execution plan
3. Executing all plan steps
4. Reviewing generated content
5. Approving and launching the campaign

### Manual Workflow
1. **Start New Campaign** (Option 1)
2. **Create Campaign Plan** (Option 2)
3. **Execute Campaign** (Option 3)
4. **Review Generated Content** (Option 6)
5. **Approve Campaign** (Option 8)
6. **Launch Campaign** (Option 9)

## Sample Campaign Flow

```
Campaign Brief: "AI-powered marketing capabilities drive 40% increase in campaign ROI"
Target Audience: "Top 20 enterprise customers"
Components: ["landing page", "email", "linkedin post", "ads"]

Generated Plan:
1. Research Audience → Customer insights for top 20 customers
2. Generate Landing Page → HTML content with AI messaging
3. Generate Email → Professional email campaign
4. Generate LinkedIn Post → Social media content
5. Generate Ads → Multi-platform advertising copy
6. Prepare Campaign Execution → Final review and launch prep
```

## Project Structure

The solution is organized into two main projects:

### AgentOrchestration (Core Library)
Contains all the core agent logic, models, and services:
```
AgentOrchestration/
├── Agents/
│   ├── IAgent.cs              # Base agent interface
│   ├── PlannerAgent.cs        # Campaign planning logic
│   ├── ResearcherAgent.cs     # Customer insights generation
│   └── RouterAgent.cs         # Orchestration and execution
├── Models/
│   └── CampaignModels.cs      # Data models for campaigns
├── Services/
│   ├── CampaignOrchestrationService.cs  # Main orchestration service
│   ├── CampaignParsingService.cs        # Campaign parsing utilities
│   └── ContextPersistenceService.cs     # Session state management
├── Tools/
│   └── ContentGenerationTools.cs       # Content generation stubs
└── AgentOrchestration.csproj           # Class library project
```

### AgentCmdClient (Command Line Interface)
Contains the interactive command-line interface:
```
AgentCmdClient/
├── Program.cs                  # Interactive demo application
├── appsettings.json           # Configuration file
└── AgentCmdClient.csproj      # Console application project
```

### Solution Structure
```
AgentMarketerPOC/
├── AgentOrchestration/         # Core library project
├── AgentCmdClient/            # Command line client project
├── AgentMarketerPOC.sln       # Solution file
├── README.md                  # This file
├── LICENSE                    # License file
└── appsettings.json          # Legacy configuration (can be removed)
```

## Technical Implementation

### Semantic Kernel Integration
- Uses Semantic Kernel for AI orchestration
- Implements function calling patterns for tool integration
- Manages conversation context and memory

### Azure OpenAI Integration
- Configured for Azure OpenAI with OpenAI fallback
- Implements proper authentication patterns
- Handles API errors gracefully

### State Management
- JSON-based session persistence
- Resumable conversation support
- Automatic cleanup of old sessions

## Limitations (Prototype)

- **Mock Data**: Customer insights use hardcoded sample data
- **Stub Content**: Generated content is placeholder text
- **Simulated Approval**: Human approval processes are automated
- **Local Storage**: Session data stored in local files
- **No Real Execution**: Campaign "launch" is simulated

## Next Steps for Production

### Core Library Enhancements
1. **Real Data Integration**: Connect to actual customer databases
2. **Advanced Content Generation**: Integrate with real content creation APIs
3. **Extensible Agent Framework**: Support for custom agent types
4. **Robust Error Handling**: Comprehensive exception management
5. **Performance Optimization**: Async patterns and caching

### Client Applications
1. **Web Interface**: ASP.NET Core web application
2. **REST API**: Web API for integration with other systems
3. **Desktop Application**: WPF or MAUI desktop client
4. **Mobile App**: Cross-platform mobile interface

### Production Infrastructure
1. **Approval Workflows**: Implement real human approval processes
2. **Cloud Storage**: Use Azure Storage or databases for persistence
3. **Campaign Execution**: Integrate with marketing automation platforms
4. **Monitoring**: Add comprehensive logging and monitoring
5. **Security**: Implement proper authentication and authorization
6. **CI/CD**: Automated build and deployment pipelines

## Contributing

This is a proof-of-concept demonstration. For production use, consider:
- Implementing proper error handling
- Adding comprehensive tests
- Securing API keys and sensitive data
- Implementing real-world integrations
- Adding monitoring and observability

## License

This project is for demonstration purposes only.
