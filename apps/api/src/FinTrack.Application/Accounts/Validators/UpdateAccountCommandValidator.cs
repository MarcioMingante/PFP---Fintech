using FinTrack.Application.Accounts.Commands.UpdateAccount;

namespace FinTrack.Application.Accounts.Validators;

public sealed class UpdateAccountCommandValidator
{
  public Dictionary<string, string[]> Validate(UpdateAccountCommand command)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(command.Name))
    {
      errors["name"] = ["Nome da conta não pode ser vazio."];
    }

    return errors;
  }
}