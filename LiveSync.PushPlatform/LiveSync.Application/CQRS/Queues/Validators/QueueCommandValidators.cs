using FluentValidation;
using LiveSync.Application.CQRS.Queues.Commands;

namespace LiveSync.Application.CQRS.Queues.Validators;

public sealed class CreateQueueCommandValidator : AbstractValidator<CreateQueueCommand>
{
    public CreateQueueCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateQueueCommandValidator : AbstractValidator<UpdateQueueCommand>
{
    public UpdateQueueCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class DeleteQueueCommandValidator : AbstractValidator<DeleteQueueCommand>
{
    public DeleteQueueCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}

public sealed class DeactivateQueueCommandValidator : AbstractValidator<DeactivateQueueCommand>
{
    public DeactivateQueueCommandValidator()
    {
        RuleFor(x => x.TenantId).GreaterThan(0);
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
