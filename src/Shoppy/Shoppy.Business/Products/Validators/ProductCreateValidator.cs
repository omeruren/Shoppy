using FluentValidation;
using Shoppy.Business.Products.DataTransferObjects;

namespace Shoppy.Business.Products.Validators;

public sealed class ProductCreateValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateValidator()
    {
        RuleFor(p => p.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(100).WithMessage("Product name can not be higher than 100 characters.");

        RuleFor(p => p.Description).MaximumLength(100).WithMessage("Description can not be higher than 100 characters.");

        RuleFor(p => p.ImageUrl).MaximumLength(2048).WithMessage("Image URL can not be higher than 2048 characters.");
    }
}
