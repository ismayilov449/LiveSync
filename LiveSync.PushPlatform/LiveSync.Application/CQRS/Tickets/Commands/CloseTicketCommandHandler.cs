using LiveSync.Application.Common.Exceptions;
using LiveSync.Application.Common.Interfaces;
using LiveSync.Domain.Interfaces;
using LiveSync.Domain.Interfaces.Repositories;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed class CloseTicketCommandHandler(
    ITicketRepository ticketRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CloseTicketCommand>
{
    public async Task Handle(CloseTicketCommand request, CancellationToken ct)
    {
        var ticket = await ticketRepository.GetByTenantAndIdAsync(request.TenantId, request.Id, ct)
            ?? throw new NotFoundException($"Ticket with ID {request.Id} was not found.");

        try
        {
            ticket.Close();
        }
        catch (InvalidOperationException ex)
        {
            throw new BusinessRuleException(ex.Message);
        }

        await ticketRepository.UpdateAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
