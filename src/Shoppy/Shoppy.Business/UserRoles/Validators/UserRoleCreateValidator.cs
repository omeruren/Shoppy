using FluentValidation;
using Shoppy.Business.UserRoles.DataTransferObjects;

namespace Shoppy.Business.UserRoles.Validators;

public sealed class UserRoleCreateValidator : AbstractValidator<UserRoleCreateDto>
{
    public UserRoleCreateValidator()
    {
        RuleFor(ur => ur.UserId).NotEmpty().WithMessage("User id is required.");
        RuleFor(ur => ur.RoleId).NotEmpty().WithMessage("Role id is required.");
    }
}
