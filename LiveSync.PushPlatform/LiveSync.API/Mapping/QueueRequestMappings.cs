using LiveSync.API.Contracts.Queues;
using LiveSync.Application.CQRS.Queues.Commands;

namespace LiveSync.API.Mapping;

public static class QueueRequestMappings
{
    public static CreateQueueCommand ToCommand(this CreateQueueRequest request, int tenantId)
        => new(tenantId, request.Name);

    public static UpdateQueueCommand ToCommand(this UpdateQueueRequest request, int tenantId, int id)
        => new(tenantId, id, request.Name);
}
