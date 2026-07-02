using FluentAssertions;
using FluentValidation.TestHelper;
using Shoppy.Business.Categories.DataTransferObjects;
using Shoppy.Business.Categories.Validators;

namespace Shoppy.UnitTests.Validators;

public class ProductCreateValidatorTests
{
    private readonly CategoryCreateValidator _validator = new();

    [Fact]
    public void Shoul_Be_Valid_When_Name_Is_Provided()
    {
        // Arrange
        var model = new CategoryCreateDto("Summer Clothes");

        // Act
        var result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Be_Invalid_When_Name_Is_EmptyOrNull(string? name)
    {
        // Arrange
        var model = new CategoryCreateDto(name!);

        // Act
        var result = _validator.Validate(model);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Category name is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_100_Characters()
    {
        // Arrange
        var request = new CategoryCreateDto(new string('a', 101));

        // Act
        var result = _validator.TestValidate(request);

        // Assert 
        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Category name can not be more than 100 characters.");
    }
    [Fact]
    public void Should_Have_Error_When_Name_Is_100_Characters()
    {
        // Arrange
        var request = new CategoryCreateDto(new string('a', 100));

        // Act
        var result = _validator.TestValidate(request);

        // Assert 
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }
}
