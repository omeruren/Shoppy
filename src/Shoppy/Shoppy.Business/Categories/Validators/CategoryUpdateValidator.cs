using FluentValidation;
using Shoppy.Business.DataTransferObjects;

namespace Shoppy.Business.Categories.Validators;

public class CategoryUpdateValidator : AbstractValidator<CategoryUpdateDto>
{
    public CategoryUpdateValidator()
    {

        RuleFor(c => c.Id).NotEmpty().WithMessage("Id is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name can not be more than 100 characters.");

    }
}
