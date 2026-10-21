using FinTrack.Application.Categories.Commands.UpdateCategory;

namespace FinTrack.Application.Categories.Validators;

public sealed class UpdateCategoryCommandValidator
{
  public Dictionary<string, string[]> Validate(UpdateCategoryCommand command)
  {
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(command.Name))
    {
      errors["name"] = ["Nome da categoria não pode ser vazio."];
    }

    return errors;
  }
}