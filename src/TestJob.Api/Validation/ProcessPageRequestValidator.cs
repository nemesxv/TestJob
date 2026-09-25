using FluentValidation;
using TestJob.Api.Models;

namespace TestJob.Api.Validation;

public sealed class ProcessPageRequestValidator : AbstractValidator<ProcessPageRequest>
{
    public ProcessPageRequestValidator()
    {
        RuleFor(x => x.Selector)
            .NotEmpty()
            .WithMessage("The selector field is required.");

        RuleFor(x => x.Attribute)
            .NotEmpty()
            .WithMessage("The attribute field is required.");

        RuleFor(x => x.UrlBase64)
            .NotEmpty()
            .WithMessage("The url_b64 field is required.");

        RuleFor(x => x.EncryptedTextBytesBase64)
            .NotEmpty()
            .WithMessage("The encrypted_text_bytes_b64 field is required.");

        RuleFor(x => x.KeyBytesBase64)
            .NotEmpty()
            .WithMessage("The key_bytes_b64 field is required.");

        RuleFor(x => x.PageBase64)
            .NotEmpty()
            .WithMessage("The page_b64 field is required.");
    }
}
