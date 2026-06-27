using FluentValidation;
using Shoppy.Business.OrderItems.Validators;
using Shoppy.Business.Orders.DataTransferObjects;

namespace Shoppy.Business.Orders.Validators;


public sealed class OrderUpdateValidator : AbstractValidator<OrderUpdateDto>
{
    public OrderUpdateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.OrderDate)
            .NotEmpty();

        RuleFor(x => x.Items)
            .NotNull()
            .Must(x => x.Any())
            .WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemUpdateValidator());
    }
}