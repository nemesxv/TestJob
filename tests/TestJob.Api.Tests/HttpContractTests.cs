using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TestJob.Api.Models;
using TestJob.Api.Services;

namespace TestJob.Api.Tests;

public sealed class HttpContractTests
{
    [Theory]
    [InlineData("url_b64", "!", "INVALID_URL_BASE64")]
    [InlineData("page_b64", "!", "INVALID_PAGE_BASE64")]
    [InlineData("key_bytes_b64", "!", "INVALID_KEY_BASE64")]
    [InlineData("encrypted_text_bytes_b64", "!", "INVALID_ENCRYPTED_TEXT_BASE64")]
    [InlineData("key_bytes_b64", "YQ==", "INVALID_ENCRYPTION_DATA")]
    [InlineData("selector", "[", "INVALID_SELECTOR")]
    [InlineData("selector", " ", "VALIDATION_ERROR")]
    [InlineData("attribute", " ", "VALIDATION_ERROR")]
    public async Task InvalidFieldsNeverPersist(string field, string value, string code)
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var payload = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(Path.Combine(root, "json_payload_1.txt")))!;
        payload[field] = value;
        using var response = await client.PostAsync("/api/process-page", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = JsonSerializer.Deserialize<ProcessPageResponse>(await response.Content.ReadAsStringAsync())!;
        Assert.Equal(code, result.ErrorCode);
        Assert.Empty(app.Store.Elements);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"selector\":123}")]
    public async Task InvalidBodyUsesRequiredEnvelope(string body)
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        using var response = await client.PostAsync("/api/process-page", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = JsonSerializer.Deserialize<ProcessPageResponse>(await response.Content.ReadAsStringAsync())!;
        Assert.Equal(1, result.IsError);
        Assert.Equal(ErrorCodes.ValidationError, result.ErrorCode);
        Assert.NotEmpty(result.ErrorMessage);
    }

    [Theory]
    [InlineData(1, 238)]
    [InlineData(2, 9)]
    public async Task SamplesProduceHttpResponses(int number, int count)
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var body = await File.ReadAllTextAsync(Path.Combine(root, $"json_payload_{number}.txt"));
        using var response = await client.PostAsync("/api/process-page", new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ProcessPageResponse>(json)!;
        Assert.Equal(0, result.IsError);
        Assert.Equal(count, result.ElementsCount);
        Assert.Equal(count, app.Store.Elements.Count);
        Assert.All(app.Store.Elements, element => Assert.StartsWith("<", element.Html));
        Assert.Contains("\n", json);
        if (Environment.GetEnvironmentVariable("UPDATE_SAMPLE_RESULTS") == "1")
            await File.WriteAllTextAsync(Path.Combine(root, $"json_result_{number}.txt"), json, new UTF8Encoding(false));
    }

    private sealed class TestApp : WebApplicationFactory<Program>
    {
        public RecordingStore Store { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IElementRepository>();
            services.AddSingleton<IElementRepository>(Store);
        });
    }

    private sealed class RecordingStore : IElementRepository
    {
        public IReadOnlyCollection<DiscoveredElement> Elements { get; private set; } = [];
        public Task InsertAsync(IReadOnlyCollection<DiscoveredElement> elements, CancellationToken cancellationToken)
        {
            Elements = elements;
            return Task.CompletedTask;
        }
    }
}
