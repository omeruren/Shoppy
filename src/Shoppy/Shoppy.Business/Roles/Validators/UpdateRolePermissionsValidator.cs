using FluentValidation;
using Shoppy.Business.Roles.DataTransferObjects;
using PermissionCatalog = Shoppy.Business.Permissions.Permissions;

namespace Shoppy.Business.Roles.Validators;

public sealed class UpdateRolePermissionsValidator : AbstractValidator<UpdateRolePermissionsDto>
{
    public UpdateRolePermissionsValidator()
    {
        RuleFor(r => r.Permissions).NotNull().WithMessage("Permissions list is required.");

        RuleForEach(r => r.Permissions)
            .Must(p => PermissionCatalog.GetAll().Contains(p))
            .WithMessage("'{PropertyValue}' is not a known permission.")
            .When(r => r.Permissions is not null);
    }
}
