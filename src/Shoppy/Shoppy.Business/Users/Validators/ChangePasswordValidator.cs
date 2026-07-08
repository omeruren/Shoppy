using FluentValidation;
using Shoppy.Business.Users.DataTransferObjects;

namespace Shoppy.Business.Users.Validators;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(c => c.CurrentPassword).NotEmpty().WithMessage("Current password is required.");

        RuleFor(c => c.NewPassword).NotEmpty().WithMessage("New password is required.");

        RuleFor(c => c.ConfirmNewPassword)
            .NotEmpty().WithMessage("Password confirmation is required.")
            .Equal(c => c.NewPassword).WithMessage("Passwords do not match.");
    }
}
