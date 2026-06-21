using LiveSync.Domain.Common;
using MediatR;

namespace LiveSync.Infrastructure.DomainEvents;

public sealed class MediatRDomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
{
    public async Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken ct = default)
    {
        foreach (var entity in entities)
        {
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();

            foreach (var domainEvent in events)
                await mediator.Publish(domainEvent, ct);
        }
    }
}