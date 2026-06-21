using FluentValidation;

namespace LiveSync.Application.Items.Commands.CreateItem;

public sealed class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.ParentId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}
