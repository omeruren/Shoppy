using FluentValidation.TestHelper;
using Shoppy.Business.Users.DataTransferObjects;
using Shoppy.Business.Users.Validators;

namespace Shoppy.UnitTests.Validators;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_Passwords_Match()
    {
        var model = new ChangePasswordDto("OldPass1!", "NewPass1!", "NewPass1!");

        var result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Confirmation_Does_Not_Match()
    {
        var model = new ChangePasswordDto("OldPass1!", "NewPass1!", "Mismatch1!");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword);
    }

    [Fact]
    public void Should_Have_Error_When_NewPassword_Is_Empty()
    {
        var model = new ChangePasswordDto("OldPass1!", "", "");

        var result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }
}
