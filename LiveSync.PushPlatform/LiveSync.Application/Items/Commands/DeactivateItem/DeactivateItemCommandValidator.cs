using FluentValidation;

namespace LiveSync.Application.Items.Commands.DeactivateItem;

public sealed class DeactivateItemCommandValidator : AbstractValidator<DeactivateItemCommand>
{
    public DeactivateItemCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
