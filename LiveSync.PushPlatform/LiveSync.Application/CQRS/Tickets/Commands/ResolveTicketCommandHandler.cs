using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed class ResolveTicketCommandHandler(
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ResolveTicketCommand>
{
    public async Task Handle(ResolveTicketCommand request, CancellationToken ct)
    {
        var ticket = await ticketRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Ticket with ID {request.Id} was not found.");

        try
        {
            ticket.Resolve();
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        await ticketRepository.UpdateAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
