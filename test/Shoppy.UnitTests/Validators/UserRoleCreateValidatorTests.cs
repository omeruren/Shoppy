using FluentValidation.TestHelper;
using Shoppy.Business.UserRoles.DataTransferObjects;
using Shoppy.Business.UserRoles.Validators;

namespace Shoppy.UnitTests.Validators;

public class UserRoleCreateValidatorTests
{
    private readonly UserRoleCreateValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_Both_Ids_Are_Provided()
    {
        var model = new UserRoleCreateDto(Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_UserId_Is_Empty()
    {
        var model = new UserRoleCreateDto(Guid.Empty, Guid.NewGuid());

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_Have_Error_When_RoleId_Is_Empty()
    {
        var model = new UserRoleCreateDto(Guid.NewGuid(), Guid.Empty);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.RoleId);
    }
}
