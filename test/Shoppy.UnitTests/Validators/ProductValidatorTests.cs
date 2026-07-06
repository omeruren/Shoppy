using FluentValidation.TestHelper;
using Shoppy.Business.Products.DataTransferObjects;
using Shoppy.Business.Products.Validators;

namespace Shoppy.UnitTests.Validators;

public class ProductDtoCreateValidatorTests
{
    private readonly ProductCreateValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_Name_Is_Provided_And_ImageUrl_Is_Null()
    {
        var request = new ProductCreateDto("Widget", null, 9.99m, Guid.NewGuid(), null);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Should_Have_Error_When_Name_Is_EmptyOrNull(string? name)
    {
        var request = new ProductCreateDto(name!, null, 9.99m, Guid.NewGuid(), null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Product name is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_100_Characters()
    {
        var request = new ProductCreateDto(new string('a', 101), null, 9.99m, Guid.NewGuid(), null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name).WithErrorMessage("Product name can not be higher than 100 characters.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_ImageUrl_Is_2048_Characters()
    {
        var request = new ProductCreateDto("Widget", null, 9.99m, Guid.NewGuid(), new string('a', 2048));

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void Should_Have_Error_When_ImageUrl_Exceeds_2048_Characters()
    {
        var request = new ProductCreateDto("Widget", null, 9.99m, Guid.NewGuid(), new string('a', 2049));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ImageUrl).WithErrorMessage("Image URL can not be higher than 2048 characters.");
    }
}

public class ProductDtoUpdateValidatorTests
{
    private readonly ProductUpdateValidator _validator = new();

    [Fact]
    public void Should_Be_Valid_When_Required_Fields_Are_Provided_And_ImageUrl_Is_Null()
    {
        var request = new ProductUpdateDto(Guid.NewGuid(), "Widget", null, 9.99m, Guid.NewGuid(), null);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Id);
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var request = new ProductUpdateDto(Guid.Empty, "Widget", null, 9.99m, Guid.NewGuid(), null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Id).WithErrorMessage("Product id is required.");
    }

    [Fact]
    public void Should_Have_Error_When_ImageUrl_Exceeds_2048_Characters()
    {
        var request = new ProductUpdateDto(Guid.NewGuid(), "Widget", null, 9.99m, Guid.NewGuid(), new string('a', 2049));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.ImageUrl).WithErrorMessage("Image URL can not be higher than 2048 characters.");
    }
}
