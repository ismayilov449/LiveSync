using LiveSync.Application.CQRS.Tickets.Services;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Entities.TicketAggregate;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed class OpenTicketCommandHandler(
    ITicketRepository ticketRepository,
    ITicketQueueValidator queueValidator,
    IUnitOfWork unitOfWork) : ICommandHandler<OpenTicketCommand, int>
{
    public async Task<int> Handle(OpenTicketCommand request, CancellationToken ct)
    {
        await queueValidator.ValidateQueueAsync(request.TenantId, request.QueueId, ct);

        var ticket = Ticket.Open(
            request.TenantId,
            request.QueueId,
            request.Subject,
            request.Description,
            request.Priority,
            request.ReporterUserId);

        await ticketRepository.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);

        ticket.CompleteCreation();
        await unitOfWork.PublishDomainEventsAsync([ticket], ct);

        return ticket.Id;
    }
}
