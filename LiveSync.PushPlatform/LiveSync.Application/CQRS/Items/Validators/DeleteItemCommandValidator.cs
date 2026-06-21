using FluentValidation;
using LiveSync.Application.CQRS.Items.Commands;

namespace LiveSync.Application.CQRS.Items.Validators;

public sealed class DeleteItemCommandValidator : AbstractValidator<DeleteItemCommand>
{
    public DeleteItemCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
