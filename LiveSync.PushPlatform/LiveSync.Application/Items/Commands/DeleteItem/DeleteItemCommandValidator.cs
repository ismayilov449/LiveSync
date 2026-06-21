using FluentValidation;

namespace LiveSync.Application.Items.Commands.DeleteItem;

public sealed class DeleteItemCommandValidator : AbstractValidator<DeleteItemCommand>
{
    public DeleteItemCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
