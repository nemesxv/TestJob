using System.Text.Json;
using TestJob.Api.Data;
using TestJob.Api.Models;
using TestJob.Api.Services;

namespace TestJob.Api.Tests;

public sealed class PageProcessingServiceTests
{
    private const string DecryptedText = "AES Error: Object reference not set to an instance of an object.";

    [Theory]
    [InlineData("json_payload_1.txt", "https://test.com/page1", 238)]
    [InlineData("json_payload_2.txt", "https://test.com/page123", 9)]
    public async Task ProcessAsync_ValidSample_ReturnsExpectedResult(
        string fileName,
        string expectedUrl,
        int expectedElementCount)
    {
        var request = await ReadRequestAsync(fileName);
        var repository = new RecordingElementRepository();
        var service = new PageProcessingService(repository);

        var response = await service.ProcessAsync(request, CancellationToken.None);

        Assert.Equal(0, response.IsError);
        Assert.Equal(string.Empty, response.ErrorCode);
        Assert.Equal(expectedUrl, response.Url);
        Assert.Equal(DecryptedText, response.DecryptedPlainText);
        Assert.Equal(expectedElementCount, response.ElementsCount);
        Assert.Equal(expectedElementCount, response.ElementsAttributeList.Count);
        Assert.Equal(expectedElementCount, repository.Elements.Count);
        Assert.Equal(response.EmailsList.Count, response.EmailsCount);
        Assert.NotEmpty(response.EmailsList);
    }

    [Fact]
    public async Task ProcessAsync_InvalidUrlBase64_ReturnsSpecificError()
    {
        var request = (await ReadRequestAsync("json_payload_1.txt")).WithUrl("not-base64");
        var service = new PageProcessingService(new RecordingElementRepository());

        var response = await service.ProcessAsync(request, CancellationToken.None);

        Assert.Equal(1, response.IsError);
        Assert.Equal(ErrorCodes.InvalidUrlBase64, response.ErrorCode);
    }

    private static async Task<ProcessPageRequest> ReadRequestAsync(string fileName)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var json = await File.ReadAllTextAsync(Path.Combine(root, fileName));
        return JsonSerializer.Deserialize<ProcessPageRequest>(json)
            ?? throw new InvalidOperationException("Не удалось десериализовать пример запроса.");
    }

    private sealed class RecordingElementRepository : IElementRepository
    {
        public IReadOnlyCollection<DiscoveredElement> Elements { get; private set; } = [];

        public Task InsertAsync(
            IReadOnlyCollection<DiscoveredElement> elements,
            CancellationToken cancellationToken)
        {
            Elements = elements;
            return Task.CompletedTask;
        }
    }
}

file static class RequestTestExtensions
{
    public static ProcessPageRequest WithUrl(this ProcessPageRequest request, string urlBase64) => new()
    {
        Selector = request.Selector,
        Attribute = request.Attribute,
        UrlBase64 = urlBase64,
        EncryptedTextBytesBase64 = request.EncryptedTextBytesBase64,
        KeyBytesBase64 = request.KeyBytesBase64,
        PageBase64 = request.PageBase64
    };
}
