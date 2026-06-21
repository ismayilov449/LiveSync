namespace LiveSync.API.Contracts.Items;

public sealed record CreateItemRequest(int ParentId, string Name);
