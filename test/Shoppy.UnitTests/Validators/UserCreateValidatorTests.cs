using FluentValidation.TestHelper;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Business.Users.Validators;

namespace Shoppy.UnitTests.Validators;

public class UserCreateValidatorTests
{
    private readonly UserCreateValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_All_Fields_Are_Provided()
    {
        var model = new UserCreateDto("Jane", "Doe", "janedoe", "jane@example.com", "Password123!");

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Have_Error_When_Email_Is_EmptyOrNull(string? email)
    {
        var model = new UserCreateDto("Jane", "Doe", "janedoe", email!, "Password123!");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Not_Valid_Format()
    {
        var model = new UserCreateDto("Jane", "Doe", "janedoe", "not-an-email", "Password123!");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Password_Is_Empty()
    {
        var model = new UserCreateDto("Jane", "Doe", "janedoe", "jane@example.com", "");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }
}
