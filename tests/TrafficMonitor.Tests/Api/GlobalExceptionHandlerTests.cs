using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TrafficMonitor.Tests.Api;

public sealed class GlobalExceptionHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestConnectionString =
        "Host=localhost;Database=traffic_monitor_tests;Username=postgres;Password=postgres";
    private readonly WebApplicationFactory<Program> _factory;

    public GlobalExceptionHandlerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetValidationSmokeRouteAsync_ReturnsProblemDetailsWithErrorsAsync()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/__smoke/validation");
        var responseBody = await response.Content.ReadAsStringAsync();
        using var problemDetails = JsonDocument.Parse(responseBody);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(400, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.True(problemDetails.RootElement.TryGetProperty("traceId", out _));
        Assert.True(problemDetails.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("name", out var nameErrors));
        Assert.Equal("name is required", nameErrors[0].GetString());
    }

    [Fact]
    public async Task GetBoomSmokeRouteAsync_ReturnsProblemDetailsWithoutLeakingExceptionAsync()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/__smoke/boom");
        var responseBody = await response.Content.ReadAsStringAsync();
        using var problemDetails = JsonDocument.Parse(responseBody);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.True(problemDetails.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("forced 500", responseBody, StringComparison.Ordinal);
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Postgres"] = TestConnectionString
                });
            });
        });
    }
}
