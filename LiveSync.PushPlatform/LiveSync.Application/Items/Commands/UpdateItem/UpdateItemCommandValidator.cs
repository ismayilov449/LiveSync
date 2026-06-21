using FluentValidation;
using LiveSync.Application.Items.Commands.UpdateItem;

namespace LiveSync.Application.Items.Commands.UpdateItem;

public sealed class UpdateItemCommandValidator : AbstractValidator<UpdateItemCommand>
{
    public UpdateItemCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}
