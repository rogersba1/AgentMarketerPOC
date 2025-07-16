# AgentMarketer Test Suite

This test project provides comprehensive testing for the AgentMarketer campaign orchestration system with intelligent LLM cost management.

## Test Structure

### Unit Tests (`Unit/`)
- **Agents/**: Tests for ModernPlannerAgent, ModernResearcherAgent, ModernContentAgent
- **Services/**: Tests for business logic services
- **Models/**: Tests for data models and validation

All unit tests use **MockLLMService** to avoid Azure Foundry costs while providing realistic LLM responses.

### Integration Tests (`Integration/`)
- **API/**: Tests for Web API endpoints with mocked dependencies
- **Orchestration/**: Tests for agent workflow coordination
- **RealLLM/**: Single test for end-to-end validation with real Azure Foundry

### Helpers (`Helpers/`)
- **MockLLMService.cs**: Intelligent mock that provides realistic responses based on input patterns

## Cost-Effective Testing Strategy

### Mock LLM Service Features
- **Pattern-based responses**: Generates contextually appropriate responses for different agent types
- **Content type awareness**: Handles email, landing page, LinkedIn, and ad copy generation
- **Company name extraction**: Personalizes responses based on company names in requests
- **Consistent outputs**: Provides deterministic responses for test repeatability

### Real LLM Integration Testing
- **Limited scope**: Only 2 tests marked with `[Fact(Skip = "Expensive - Run manually only")]`
- **Manual execution**: Disabled by default to prevent accidental Azure charges
- **Performance validation**: Measures response times and validates end-to-end functionality
- **Budget awareness**: Includes configuration for cost monitoring

## Running Tests

### Standard Test Suite (No Azure costs)
```bash
# Run all unit and integration tests with mocked LLM
dotnet test --filter "Category!=RealLLM"

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests with mocks
dotnet test --filter "Category=Integration&Category!=RealLLM"
```

### Real LLM Tests (Azure costs apply)
```bash
# Enable real LLM tests by removing Skip attribute, then:
dotnet test --filter "Category=RealLLM"

# Or run specific real LLM test
dotnet test --filter "FullyQualifiedName~RealLLMIntegrationTests.PlannerAgent_ShouldWorkWithRealLLM"
```

## Test Coverage

### ModernPlannerAgent Tests
- ✅ Campaign request parsing and parameter extraction
- ✅ Industry identification (healthcare, finance, manufacturing, etc.)
- ✅ Company count extraction (1, 5, 10, etc.)
- ✅ Content type detection (email, landing page, LinkedIn, ad copy)
- ✅ Execution plan generation
- ✅ Confidence scoring
- ✅ Error handling and edge cases

### ModernResearcherAgent Tests
- ✅ Company brief generation with structured content
- ✅ Industry-specific insights and recommendations
- ✅ Business metrics and market positioning
- ✅ Marketing channel recommendations
- ✅ Decision maker identification
- ✅ Actionable campaign guidance

### ModernContentAgent Tests
- ✅ Email content with proper structure (subject, body, CTA)
- ✅ Landing page with hero section, value props, CTA
- ✅ LinkedIn posts with engaging format and hashtags
- ✅ Ad copy with headlines, body copy, and CTAs
- ✅ Content personalization based on company names
- ✅ Consistent value propositions across content types

### API Integration Tests
- ✅ Campaign creation endpoint validation
- ✅ Company brief generation endpoint
- ✅ Content generation endpoint for all types
- ✅ Error handling and validation
- ✅ Concurrent request handling
- ✅ Large request processing

## Configuration

### Test Settings (`appsettings.json`)
```json
{
  "Testing": {
    "UseMockLLM": true,
    "EnableRealLLMTests": false,
    "RealLLMTestBudget": {
      "MaxRequestsPerDay": 10,
      "MaxCostPerMonth": 25.00
    }
  }
}
```

### Azure Foundry Configuration (for real LLM tests)
Update `appsettings.json` with your Azure Foundry credentials:
```json
{
  "AzureFoundry": {
    "Endpoint": "https://your-endpoint.com",
    "ApiKey": "your-api-key",
    "ModelName": "gpt-4o"
  }
}
```

## Mock LLM Behavior

The MockLLMService provides intelligent responses based on input analysis:

### Campaign Planning Responses
- Extracts audience, company count, and content components
- Generates structured execution plans
- Provides confidence scores and recommendations

### Company Research Responses
- Creates detailed company briefs with business metrics
- Includes marketing insights and channel recommendations
- Provides actionable campaign guidance

### Content Generation Responses
- Generates personalized content for specific companies
- Maintains consistent messaging and value propositions
- Includes proper formatting for each content type

## Best Practices

1. **Run mocked tests frequently** during development
2. **Use real LLM tests sparingly** for critical validation
3. **Monitor Azure costs** when running real LLM tests
4. **Update mock responses** when adding new agent capabilities
5. **Validate performance** with real LLM tests before production

## Contributing

When adding new tests:
1. Use MockLLMService for unit and most integration tests
2. Add real LLM tests only for critical end-to-end validation
3. Mark expensive tests with appropriate Skip attributes
4. Update mock service when adding new agent functionality
5. Document any new test patterns or approaches
