using System.Text.Json.Serialization;

namespace TestJob.Api.Models;

public sealed class ProcessPageRequest
{
    [JsonPropertyName("selector")]
    public string? Selector { get; init; }

    [JsonPropertyName("attribute")]
    public string? Attribute { get; init; }

    [JsonPropertyName("url_b64")]
    public string? UrlBase64 { get; init; }

    [JsonPropertyName("encrypted_text_bytes_b64")]
    public string? EncryptedTextBytesBase64 { get; init; }

    [JsonPropertyName("key_bytes_b64")]
    public string? KeyBytesBase64 { get; init; }

    [JsonPropertyName("page_b64")]
    public string? PageBase64 { get; init; }
}

public sealed class ProcessPageResponse
{
    [JsonPropertyName("is_error")]
    public int IsError { get; init; }

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; init; } = string.Empty;

    [JsonPropertyName("error_message")]
    public string ErrorMessage { get; init; } = string.Empty;

    [JsonPropertyName("elements_count")]
    public int ElementsCount { get; init; }

    [JsonPropertyName("emails_count")]
    public int EmailsCount { get; init; }

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("decrypted_plain_text")]
    public string DecryptedPlainText { get; init; } = string.Empty;

    [JsonPropertyName("elements_attr_list")]
    public IReadOnlyList<string> ElementsAttributeList { get; init; } = [];

    [JsonPropertyName("emails_list")]
    public IReadOnlyList<string> EmailsList { get; init; } = [];

    public static ProcessPageResponse Error(string code, string message) => new()
    {
        IsError = 1,
        ErrorCode = code,
        ErrorMessage = message
    };
}

public sealed record DiscoveredElement(string AttributeValue, string Html);

public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidUrlBase64 = "INVALID_URL_BASE64";
    public const string InvalidPageBase64 = "INVALID_PAGE_BASE64";
    public const string InvalidKeyBase64 = "INVALID_KEY_BASE64";
    public const string InvalidEncryptedTextBase64 = "INVALID_ENCRYPTED_TEXT_BASE64";
    public const string InvalidEncryptionData = "INVALID_ENCRYPTION_DATA";
    public const string InvalidSelector = "INVALID_SELECTOR";
    public const string UnexpectedError = "UNEXPECTED_ERROR";
}
