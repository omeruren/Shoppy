using FluentValidation;
using Shoppy.Business.Auth.DataTransferObjects;

namespace Shoppy.Business.Auth.Validators;

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Reset code is required.")
            .Length(6).WithMessage("Reset code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Reset code must contain only digits.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one number.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}