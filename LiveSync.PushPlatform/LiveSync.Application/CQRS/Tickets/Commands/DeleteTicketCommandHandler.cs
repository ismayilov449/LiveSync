using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed class DeleteTicketCommandHandler(
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteTicketCommand>
{
    public async Task Handle(DeleteTicketCommand request, CancellationToken ct)
    {
        var ticket = await ticketRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Ticket with ID {request.Id} was not found.");

        ticket.MarkDeleted();

        await ticketRepository.DeleteAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
