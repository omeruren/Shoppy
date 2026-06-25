using FluentValidation;
using Shoppy.Business.Products.DataTransferObjects;

namespace Shoppy.Business.Products.Validators;

public sealed class ProductUpdateValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateValidator()
    {
        RuleFor(p => p.Id).NotEmpty().WithMessage("Product id is required.");

        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(100).WithMessage("Product name can not be higher than 100 characters.");

        RuleFor(p => p.Description).MaximumLength(100).WithMessage("Description can not be higher than 100 characters.");
    }
}
