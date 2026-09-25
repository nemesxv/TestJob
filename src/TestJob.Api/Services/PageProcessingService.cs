using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using TestJob.Api.Data;
using TestJob.Api.Models;

namespace TestJob.Api.Services;

public interface IPageProcessingService
{
    Task<ProcessPageResponse> ProcessAsync(ProcessPageRequest request, CancellationToken cancellationToken);
}

public sealed class PageProcessingService(IElementRepository elementRepository) : IPageProcessingService
{
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
            var document = await parser.ParseDocumentAsync(page, cancellationToken);

            IHtmlCollection<IElement> selectedElements;
            try
            {
                selectedElements = document.QuerySelectorAll(request.Selector!);
            }
            catch (Exception exception) when (exception is DomException or ArgumentException)
            {
                throw new ProcessingException(ErrorCodes.InvalidSelector, exception.Message, exception);
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
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
        catch (FormatException exception)
        {
            throw new ProcessingException(errorCode, exception.Message, exception);
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
            throw new ProcessingException(ErrorCodes.InvalidEncryptedTextBase64, exception.Message, exception);
        }

        try
        {
            keyBytes = Convert.FromBase64String(keyBase64);
        }
        catch (FormatException exception)
        {
            throw new ProcessingException(ErrorCodes.InvalidKeyBase64, exception.Message, exception);
        }

        if (keyBytes.Length != 32 || encryptedBytes.Length == 0 || encryptedBytes.Length % 16 != 0)
        {
            throw new ProcessingException(
                ErrorCodes.InvalidEncryptionData,
                "AES-256 requires a 32-byte key and non-empty ciphertext aligned to a 16-byte block.");
        }

        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Key = keyBytes;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;

            return Encoding.UTF8.GetString(aes.DecryptEcb(encryptedBytes, PaddingMode.None));
        }
        catch (CryptographicException exception)
        {
            throw new ProcessingException(ErrorCodes.InvalidEncryptionData, exception.Message, exception);
        }
    }

    private sealed class ProcessingException(string code, string message, Exception? innerException = null)
        : Exception(message, innerException)
    {
        public string Code { get; } = code;
    }
}
