using FluentValidation;
using TestJob.Api.Models;

namespace TestJob.Api.Validation;

public sealed class ProcessPageRequestValidator : AbstractValidator<ProcessPageRequest>
{
    public ProcessPageRequestValidator()
    {
        RuleFor(x => x.Selector)
            .NotEmpty()
            .WithMessage("Поле selector обязательно.");

        RuleFor(x => x.Attribute)
            .NotEmpty()
            .WithMessage("Поле attribute обязательно.");

        RuleFor(x => x.UrlBase64)
            .NotEmpty()
            .WithMessage("Поле url_b64 обязательно.");

        RuleFor(x => x.EncryptedTextBytesBase64)
            .NotEmpty()
            .WithMessage("Поле encrypted_text_bytes_b64 обязательно.");

        RuleFor(x => x.KeyBytesBase64)
            .NotEmpty()
            .WithMessage("Поле key_bytes_b64 обязательно.");

        RuleFor(x => x.PageBase64)
            .NotEmpty()
            .WithMessage("Поле page_b64 обязательно.");
    }
}
