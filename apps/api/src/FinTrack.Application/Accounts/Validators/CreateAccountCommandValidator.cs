using FinTrack.Application.Accounts.Commands.CreateAccount;

namespace FinTrack.Application.Accounts.Validators;

public sealed class CreateAccountCommandValidator
{
  public Dictionary<string, string[]> Validate(CreateAccountCommand command)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(command.Name))
    {
      errors["name"] = ["Nome da conta não pode ser vazio."];
    }

    return errors;
  }
}
