using FluentValidation;
using Shoppy.Business.OrderItems.DataTransferObjects;

namespace Shoppy.Business.OrderItems.Validators;

public sealed class OrderItemUpdateValidator : AbstractValidator<OrderItemUpdateDto>
{
    public OrderItemUpdateValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.ProductId)
            .NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0);
    }
}