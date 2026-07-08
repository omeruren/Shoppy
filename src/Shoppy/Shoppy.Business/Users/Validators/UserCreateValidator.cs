using FluentValidation;
using Shoppy.Business.Users.DataTransferObjects;

namespace Shoppy.Business.Users.Validators;

public sealed class UserCreateValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateValidator()
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

        RuleFor(u => u.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(u => u.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
