using FluentValidation;
using App.Models;

namespace App.Validators;

public class ProcessRequestValidator : AbstractValidator<ProcessRequest>
{
    public ProcessRequestValidator()
    {
        RuleFor(x => x.Selector)
            .NotEmpty().WithErrorCode("EMPTY_SELECTOR")
            .WithMessage("selector must not be empty");

        RuleFor(x => x.Attribute)
            .NotEmpty().WithErrorCode("EMPTY_ATTRIBUTE")
            .WithMessage("attribute must not be empty");

        RuleFor(x => x.Url_b64)
            .NotEmpty().WithErrorCode("MISSING_URL")
            .WithMessage("url_b64 is required")
            .Must(BeValidBase64).WithErrorCode("INVALID_URL_BASE64")
            .WithMessage("url_b64 is not valid base64");

        RuleFor(x => x.Page_b64)
            .NotEmpty().WithErrorCode("MISSING_PAGE")
            .WithMessage("page_b64 is required")
            .Must(BeValidBase64).WithErrorCode("INVALID_PAGE_BASE64")
            .WithMessage("page_b64 is not valid base64");

        RuleFor(x => x.Encrypted_text_bytes_b64)
            .NotEmpty().WithErrorCode("MISSING_ENCRYPTED_TEXT")
            .WithMessage("encrypted_text_bytes_b64 is required")
            .Must(BeValidBase64).WithErrorCode("INVALID_ENCRYPTED_TEXT_BASE64")
            .WithMessage("encrypted_text_bytes_b64 is not valid base64");

        RuleFor(x => x.Key_bytes_b64)
            .NotEmpty().WithErrorCode("MISSING_KEY")
            .WithMessage("key_bytes_b64 is required")
            .Must(BeValidBase64).WithErrorCode("INVALID_KEY_BASE64")
            .WithMessage("key_bytes_b64 is not valid base64")
            .Must(k => Convert.FromBase64String(k!).Length == 32)
                .WithErrorCode("INVALID_KEY_LENGTH")
                .WithMessage("key_bytes_b64 must decode to 32 bytes (AES-256)");
    }

    private static bool BeValidBase64(string? value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        Span<byte> buffer = new byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}
