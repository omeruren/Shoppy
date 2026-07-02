using FluentValidation;
using Shoppy.Business.OrderItems.Validators;
using Shoppy.Business.Orders.DataTransferObjects;

namespace Shoppy.Business.Orders.Validators;

public sealed class OrderCreateValidator : AbstractValidator<OrderCreateDto>
{
    public OrderCreateValidator()
    {

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemCreateValidator());
    }
}