using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Dapper;
using Npgsql;
using TestJob.Api.Models;

namespace TestJob.Api.Services;

public interface IPageProcessingService
{
    Task<ProcessPageResponse> ProcessAsync(ProcessPageRequest request, CancellationToken cancellationToken);
}

public interface IElementRepository
{
    Task InsertAsync(IReadOnlyCollection<DiscoveredElement> elements, CancellationToken cancellationToken);
}

public sealed class ElementRepository(NpgsqlDataSource dataSource) : IElementRepository
{
    private const string InsertSql = """
        INSERT INTO elements (attribute_value, html)
        VALUES (@AttributeValue, @Html);
        """;

    public async Task InsertAsync(
        IReadOnlyCollection<DiscoveredElement> elements,
        CancellationToken cancellationToken)
    {
        if (elements.Count == 0)
            return;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            InsertSql,
            elements,
            cancellationToken: cancellationToken));
    }
}

public sealed class PageProcessingService(IElementRepository elementRepository) : IPageProcessingService
{
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(false, true);
    private static readonly Regex EmailRegex = new(
        @"(?<![\w.!#$%&'*+/=?^`{|}~-])[A-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?(?:\.[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?)+(?![\w-])",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    public async Task<ProcessPageResponse> ProcessAsync(
        ProcessPageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = DecodeUtf8(request.UrlBase64!, ErrorCodes.InvalidUrlBase64);
            var page = DecodeUtf8(request.PageBase64!, ErrorCodes.InvalidPageBase64);
            var decryptedText = Decrypt(request.EncryptedTextBytesBase64!, request.KeyBytesBase64!);

            var parser = new HtmlParser();
            using var document = await parser.ParseDocumentAsync(page, cancellationToken);

            IHtmlCollection<IElement> selectedElements;
            try
            {
                selectedElements = document.QuerySelectorAll(request.Selector!);
            }
            catch (Exception exception) when (exception is DomException or ArgumentException)
            {
                throw new ProcessingException(ErrorCodes.InvalidSelector, "Некорректный CSS-селектор.", exception);
            }

            var elements = selectedElements
                .Select(element => new DiscoveredElement(
                    element.GetAttribute(request.Attribute!) ?? string.Empty,
                    element.OuterHtml))
                .ToArray();

            var emails = EmailRegex.Matches(page)
                .Select(match => match.Value)
                .ToArray();

            await elementRepository.InsertAsync(elements, cancellationToken);

            return new ProcessPageResponse
            {
                ElementsCount = elements.Length,
                EmailsCount = emails.Length,
                Url = url,
                DecryptedPlainText = decryptedText,
                ElementsAttributeList = elements.Select(element => element.AttributeValue).ToArray(),
                EmailsList = emails
            };
        }
        catch (ProcessingException exception)
        {
            return ProcessPageResponse.Error(exception.Code, exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return ProcessPageResponse.Error(ErrorCodes.UnexpectedError, exception.Message);
        }
    }

    private static string DecodeUtf8(string value, string errorCode)
    {
        try
        {
            return StrictUtf8.GetString(Convert.FromBase64String(value));
        }
        catch (Exception exception) when (exception is FormatException or DecoderFallbackException)
        {
            var message = errorCode == ErrorCodes.InvalidUrlBase64
                ? "Поле url_b64 должно содержать корректную Base64-строку в кодировке UTF-8."
                : "Поле page_b64 должно содержать корректную Base64-строку в кодировке UTF-8.";

            throw new ProcessingException(errorCode, message, exception);
        }
    }

    private static string Decrypt(string encryptedTextBase64, string keyBase64)
    {
        byte[] encryptedBytes;
        byte[] keyBytes;

        try
        {
            encryptedBytes = Convert.FromBase64String(encryptedTextBase64);
        }
        catch (FormatException exception)
        {
            throw new ProcessingException(
                ErrorCodes.InvalidEncryptedTextBase64,
                "Поле encrypted_text_bytes_b64 должно содержать корректную Base64-строку.",
                exception);
        }

        try
        {
            keyBytes = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException exception)
        {
            throw new ProcessingException(
                ErrorCodes.InvalidKeyBase64,
                "Поле key_bytes_b64 должно содержать корректную Base64-строку.",
                exception);
        }

        if (keyBytes.Length != 32 || encryptedBytes.Length == 0 || encryptedBytes.Length % 16 != 0)
        {
            throw new ProcessingException(
                ErrorCodes.InvalidEncryptionData,
                "Для AES-256 требуется ключ длиной 32 байта и непустой шифротекст, длина которого кратна блоку 16 байт.");
        }

        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = keyBytes;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            return StrictUtf8.GetString(aes.DecryptEcb(encryptedBytes, PaddingMode.None));
        }
        catch (Exception exception) when (exception is CryptographicException or DecoderFallbackException)
        {
            throw new ProcessingException(
                ErrorCodes.InvalidEncryptionData,
                "Не удалось расшифровать данные AES-256 или преобразовать результат в UTF-8.",
                exception);
        }
    }

    private sealed class ProcessingException(string code, string message, Exception? innerException = null)
        : Exception(message, innerException)
    {
        public string Code { get; } = code;
    }
}
