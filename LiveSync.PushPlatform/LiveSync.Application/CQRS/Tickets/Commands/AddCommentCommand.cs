using LiveSync.Application.Common.Interfaces;

namespace LiveSync.Application.CQRS.Tickets.Commands;

public sealed record AddCommentCommand(int TenantId, int Id, int AuthorUserId, string Body) : ICommand;
