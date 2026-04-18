using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TrafficMonitor.Tests.Api;

public sealed class GlobalExceptionHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    static GlobalExceptionHandlerTests()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Database=traffic_monitor_tests;Username=postgres;Password=postgres");
    }

    public GlobalExceptionHandlerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetValidationSmokeRouteAsync_ReturnsProblemDetailsWithErrorsAsync()
    {
        using var client = _factory.CreateClient();
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
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/__smoke/boom");
        var responseBody = await response.Content.ReadAsStringAsync();
        using var problemDetails = JsonDocument.Parse(responseBody);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, problemDetails.RootElement.GetProperty("status").GetInt32());
        Assert.True(problemDetails.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("forced 500", responseBody, StringComparison.Ordinal);
    }
}
