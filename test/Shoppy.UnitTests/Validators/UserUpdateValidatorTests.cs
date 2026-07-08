using FluentValidation.TestHelper;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Business.Users.Validators;

namespace Shoppy.UnitTests.Validators;

public class UserUpdateValidatorTests
{
    private readonly UserUpdateValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_All_Fields_Are_Provided()
    {
        var model = new UserUpdateDto(Guid.NewGuid(), "Jane", "Doe", "janedoe", "jane@example.com");

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var model = new UserUpdateDto(Guid.Empty, "Jane", "Doe", "janedoe", "jane@example.com");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Not_Valid_Format()
    {
        var model = new UserUpdateDto(Guid.NewGuid(), "Jane", "Doe", "janedoe", "not-an-email");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}
