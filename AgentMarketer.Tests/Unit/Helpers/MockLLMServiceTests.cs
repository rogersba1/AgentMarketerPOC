using Xunit;
using FluentAssertions;
using AgentMarketer.Tests.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentMarketer.Tests.Unit.Helpers
{
    public class MockLLMServiceTests
    {
        private readonly MockLLMService _mockLLMService;

        public MockLLMServiceTests()
        {
            _mockLLMService = new MockLLMService();
        }

        [Fact]
        public async Task MockLLMService_ShouldGenerateCampaignParsing_WhenGivenCampaignInput()
        {
            // Arrange
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddUserMessage("Parse this campaign: Create a campaign for 3 manufacturing companies with email and LinkedIn content");

            // Act
            var result = await _mockLLMService.GetChatMessageContentsAsync(chatHistory);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var response = result[0].Content;
            response.Should().NotBeNullOrEmpty();
            response.Should().Contain("Parsed Campaign Parameters");
            response.Should().Contain("manufacturing");
        }

        [Fact]
        public async Task MockLLMService_ShouldGenerateCompanyBrief_WhenGivenResearchInput()
        {
            // Arrange
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddUserMessage("Generate a company brief for TechCorp Solutions in the technology industry");

            // Act
            var result = await _mockLLMService.GetChatMessageContentsAsync(chatHistory);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var response = result[0].Content;
            response.Should().NotBeNullOrEmpty();
            response.Should().Contain("Company Brief: TechCorp Solutions");
            response.Should().Contain("Company Overview");
            response.Should().Contain("Marketing Insights");
        }

        [Theory]
        [InlineData("email", "Subject:")]
        [InlineData("landing page", "# Personalized Landing Page")]
        [InlineData("linkedin", "🚀")]
        [InlineData("ad copy", "**Headline:**")]
        public async Task MockLLMService_ShouldGenerateCorrectContentType(string contentType, string expectedIndicator)
        {
            // Arrange
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddUserMessage($"Generate {contentType} content for Test Company");

            // Act
            var result = await _mockLLMService.GetChatMessageContentsAsync(chatHistory);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var response = result[0].Content;
            response.Should().NotBeNullOrEmpty();
            response.Should().Contain(expectedIndicator);
            response.Should().Contain("Test Company");
        }

        [Fact]
        public async Task MockLLMService_ShouldPersonalizeResponses_BasedOnCompanyName()
        {
            // Arrange
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddUserMessage("Create email content for Unique Corporation");

            // Act
            var result = await _mockLLMService.GetChatMessageContentsAsync(chatHistory);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var response = result[0].Content;
            response.Should().NotBeNullOrEmpty();
            response.Should().Contain("Unique Corporation");
        }

        [Fact]
        public async Task MockLLMService_ShouldReturnDefaultResponse_ForUnknownInput()
        {
            // Arrange
            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddUserMessage("Random unrecognized input that doesn't match any patterns");

            // Act
            var result = await _mockLLMService.GetChatMessageContentsAsync(chatHistory);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            var response = result[0].Content;
            response.Should().NotBeNullOrEmpty();
            response.Should().Contain("Campaign Planner Agent");
        }

        [Fact]
        public async Task MockLLMService_ShouldProduceConsistentResults()
        {
            // Arrange
            var chatHistory1 = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            var chatHistory2 = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory1.AddUserMessage("Parse campaign: 3 technology companies with email");
            chatHistory2.AddUserMessage("Parse campaign: 3 technology companies with email");

            // Act
            var result1 = await _mockLLMService.GetChatMessageContentsAsync(chatHistory1);
            var result2 = await _mockLLMService.GetChatMessageContentsAsync(chatHistory2);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result1[0].Content.Should().Contain("technology");
            result2[0].Content.Should().Contain("technology");
            result1[0].Content.Should().Contain("3");
            result2[0].Content.Should().Contain("3");
        }
    }
}
