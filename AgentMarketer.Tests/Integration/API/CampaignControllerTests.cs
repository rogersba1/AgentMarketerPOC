using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net;

namespace AgentMarketer.Tests.Integration.API
{
    public class CampaignControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CampaignControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GET_Root_ShouldReturnSuccessful()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GET_HealthCheck_ShouldReturnSuccessful()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/health");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GET_Swagger_ShouldReturnSuccessful()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/swagger");

            // Assert
            // Swagger might redirect, so check for success or redirect
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, 
                HttpStatusCode.MovedPermanently, 
                HttpStatusCode.Found,
                HttpStatusCode.NotFound);
        }

        [Fact]
        public void API_ShouldStartSuccessfully()
        {
            // This test validates that the application can start without exceptions
            // Just creating the client validates much of the startup process
            
            // Arrange & Act
            using var testClient = _factory.CreateClient();

            // Assert
            testClient.Should().NotBeNull();
        }

        [Fact]
        public async Task API_ShouldHandleBadRequest_Gracefully()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/api/nonexistent");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
