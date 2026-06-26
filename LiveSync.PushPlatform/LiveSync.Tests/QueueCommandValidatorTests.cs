using FluentValidation.TestHelper;
using LiveSync.Application.CQRS.Queues.Commands;
using LiveSync.Application.CQRS.Queues.Validators;

namespace LiveSync.Tests;

public sealed class QueueCommandValidatorTests
{
  private readonly CreateQueueCommandValidator _createValidator = new();
  private readonly UpdateQueueCommandValidator _updateValidator = new();

  [Fact]
  public void CreateQueue_WithValidData_PassesValidation()
  {
    var result = _createValidator.TestValidate(new CreateQueueCommand(1, "Support"));
    result.ShouldNotHaveAnyValidationErrors();
  }

  [Fact]
  public void CreateQueue_WithEmptyName_FailsValidation()
  {
    var result = _createValidator.TestValidate(new CreateQueueCommand(1, ""));
    result.ShouldHaveValidationErrorFor(x => x.Name);
  }

  [Fact]
  public void UpdateQueue_WithInvalidTenantId_FailsValidation()
  {
    var result = _updateValidator.TestValidate(new UpdateQueueCommand(0, 1, "Renamed"));
    result.ShouldHaveValidationErrorFor(x => x.TenantId);
  }
}
