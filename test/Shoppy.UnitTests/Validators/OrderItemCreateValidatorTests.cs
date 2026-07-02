using FluentValidation.TestHelper;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.OrderItems.Validators;

namespace Shoppy.UnitTests.Validators;

public sealed class OrderItemCreateValidatorTests
{
    private readonly OrderItemCreateValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_ProductId_Is_Empty()
    {
        // Arrange
        var dto = new OrderItemCreateDto(Guid.Empty, 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
              .WithErrorMessage("Product is required.");
    }

    [Fact]
    public void Should_Have_Error_When_Quantity_Is_Zero()
    {
        // Arrange
        var dto = new OrderItemCreateDto(Guid.NewGuid(), 0);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
              .WithErrorMessage("Quantity must be greater than zero.");
    }

    [Fact]
    public void Should_Have_Error_When_Quantity_Is_Negative()
    {
        // Arrange
        var dto = new OrderItemCreateDto(Guid.NewGuid(), -5);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Request_Is_Valid()
    {
        // Arrange
        var dto = new OrderItemCreateDto(Guid.NewGuid(), 5);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}