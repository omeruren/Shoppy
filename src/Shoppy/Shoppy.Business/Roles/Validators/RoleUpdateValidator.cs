using FluentValidation;
using Shoppy.Business.Roles.DataTransferObjects;

namespace Shoppy.Business.Roles.Validators;

public sealed class RoleUpdateValidator : AbstractValidator<RoleUpdateDto>
{
    public RoleUpdateValidator()
    {
        RuleFor(r => r.Id).NotEmpty().WithMessage("id is required");
        RuleFor(r => r.Name).NotEmpty().WithMessage("Name is required");

    }
}
