using FluentValidation.TestHelper;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Business.Users.Validators;

namespace Shoppy.UnitTests.Validators;

public class UserUpdateSelfValidatorTests
{
    private readonly UserUpdateSelfValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_All_Fields_Are_Provided()
    {
        var model = new UserUpdateSelfDto("Jane", "Doe", "janedoe");

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Have_Error_When_UserName_Is_EmptyOrNull(string? userName)
    {
        var model = new UserUpdateSelfDto("Jane", "Doe", userName!);

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }
}
