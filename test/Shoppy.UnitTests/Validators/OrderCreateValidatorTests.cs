using FluentValidation.TestHelper;
using Shoppy.Business.OrderItems.DataTransferObjects;
using Shoppy.Business.Orders.DataTransferObjects;
using Shoppy.Business.Orders.Validators;

namespace Shoppy.UnitTests.Validators;


public sealed class OrderCreateValidatorTests
{
    private readonly OrderCreateValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Items_Is_Empty()
    {
        // Arrange
        var dto = new OrderCreateDto([]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Items)
              .WithErrorMessage("Order must contain at least one item.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Items_Is_Valid()
    {
        // Arrange
        var dto = new OrderCreateDto(
        [
            new OrderItemCreateDto(Guid.NewGuid(), 2)
        ]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Should_Have_Error_When_OrderItem_Is_Invalid()
    {
        // Arrange
        var dto = new OrderCreateDto(
        [
            new OrderItemCreateDto(Guid.Empty, 0)
        ]);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Items);
    }
}
