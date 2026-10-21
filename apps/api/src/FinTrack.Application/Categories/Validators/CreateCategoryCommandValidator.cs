using FinTrack.Application.Categories.Commands.CreateCategory;

namespace FinTrack.Application.Categories.Validators;

public sealed class CreateCategoryCommandValidator
{
  public Dictionary<string, string[]> Validate(
    CreateCategoryCommand command
  )
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(command.Name))
    {
      errors["name"] = ["Nome da categoria não pode ser vazio."];
    }

    return errors;
  }
}