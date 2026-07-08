using FluentValidation.TestHelper;
using Shoppy.Business.Permissions;
using Shoppy.Business.Roles.DataTransferObjects;
using Shoppy.Business.Roles.Validators;

namespace Shoppy.UnitTests.Validators;

public class UpdateRolePermissionsValidatorTests
{
    private readonly UpdateRolePermissionsValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_All_Permissions_Are_Known()
    {
        var model = new UpdateRolePermissionsDto([Permissions.Products.Read, Permissions.Categories.Read]);

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Be_Valid_When_Permissions_List_Is_Empty()
    {
        var model = new UpdateRolePermissionsDto([]);

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Permission_Is_Unknown()
    {
        var model = new UpdateRolePermissionsDto(["NotARealPermission.Read"]);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor("Permissions[0]");
    }
}
