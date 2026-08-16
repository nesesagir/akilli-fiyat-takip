using FluentValidation;
using PriceTracker.Application.DTOs;

namespace PriceTracker.Application.Validators;

public class CreateTrackedItemRequestValidator : AbstractValidator<CreateTrackedItemRequest>
{
    public CreateTrackedItemRequestValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ProductUrl)
            .NotEmpty()
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Geçerli bir ürün URL'i girin.");
        RuleFor(x => x.TargetPrice).GreaterThan(0);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
            .MinimumLength(8)
            .WithMessage("Şifre en az 8 karakter olmalı.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public class LoginUserRequestValidator : AbstractValidator<LoginUserRequest>
{
    public LoginUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class UpdateUserProfileRequestValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PreferredCurrency)
            .NotEmpty()
            .Must(c => c is "TRY" or "USD" or "EUR" or "GBP")
            .WithMessage("Para birimi TRY, USD, EUR veya GBP olmalı.");
        RuleFor(x => x.PreferredLanguage)
            .NotEmpty()
            .Must(l => l is "tr" or "en")
            .WithMessage("Dil tr veya en olmalı.");
    }
}
