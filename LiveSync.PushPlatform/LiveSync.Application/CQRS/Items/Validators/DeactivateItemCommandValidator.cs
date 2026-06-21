using FluentValidation;
using LiveSync.Application.CQRS.Items.Commands;

namespace LiveSync.Application.CQRS.Items.Validators;

public sealed class DeactivateItemCommandValidator : AbstractValidator<DeactivateItemCommand>
{
    public DeactivateItemCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
