namespace FinTrack.Domain.Categories;

public sealed class Category
{
  private Category(){}

  public Category(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException(
        "Nome da categoria não pode ser vazio.",
        nameof(name)
      );
    }

    Id = Guid.NewGuid();
    Name = name.Trim();
  }

  public Guid Id { get; private set; }

  public string Name { get; private set; } = string.Empty;

  public void Rename(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException(
        "Nome da categoria não pode ser vazio.",
        nameof(name)
      );
    }

    Name = name.Trim();
  }
}