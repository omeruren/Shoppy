using FluentValidation;
using Shoppy.Business.Users.DataTransferObjects;

namespace Shoppy.Business.Users.Validators;

public sealed class UserUpdateSelfValidator : AbstractValidator<UserUpdateSelfDto>
{
    public UserUpdateSelfValidator()
    {
        RuleFor(u => u.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name can not be higher than 100 characters.");

        RuleFor(u => u.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name can not be higher than 100 characters.");

        RuleFor(u => u.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(100).WithMessage("Username can not be higher than 100 characters.");
    }
}
