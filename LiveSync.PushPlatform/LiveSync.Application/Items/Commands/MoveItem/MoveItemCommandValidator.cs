using FluentValidation;

namespace LiveSync.Application.Items.Commands.MoveItem;

public sealed class MoveItemCommandValidator : AbstractValidator<MoveItemCommand>
{
    public MoveItemCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ParentId).GreaterThan(0);
    }
}
