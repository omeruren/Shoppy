using FluentValidation;
using Shoppy.Business.Auth.DataTransferObjects;

namespace Shoppy.Business.Auth.Validators;

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}