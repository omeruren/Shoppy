using FluentValidation;
using Shoppy.Business.Categories.DataTransferObjects;

namespace Shoppy.Business.Categories.Validators;

public class CategoryCreateValidator : AbstractValidator<CategoryCreateDto>
{
    public CategoryCreateValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name can not be more than 100 characters.");

    }
}