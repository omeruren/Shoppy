using FluentValidation;
using Shoppy.Business.Roles.DataTransferObjects;

namespace Shoppy.Business.Roles.Validators;

public sealed class RoleCreateValidator : AbstractValidator<RoleCreateDto>
{
    public RoleCreateValidator()
    {
        RuleFor(r => r.Name).NotEmpty().WithMessage("Role name is required.");
    }
}
