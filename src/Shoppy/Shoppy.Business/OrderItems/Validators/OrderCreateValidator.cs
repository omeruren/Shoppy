using FluentValidation;
using Shoppy.Business.OrderItems.DataTransferObjects;

namespace Shoppy.Business.OrderItems.Validators;

public sealed class OrderItemCreateValidator : AbstractValidator<OrderItemCreateDto>
{
    public OrderItemCreateValidator()
    {
        RuleFor(x => x.ProductId)
           .NotEmpty()
           .WithMessage("Product is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");
    }
}