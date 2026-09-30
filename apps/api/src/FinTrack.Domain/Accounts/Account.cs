namespace FinTrack.Domain.Accounts;

public sealed class Account
{
  private Account(){}

  public Account(string name)
  {
    if (string.IsNullOrWhiteSpace(name))
    {
      throw new ArgumentException(
        "Nome da conta não pode ser vazio.",
        nameof(name)
      );
    }

    Id = Guid.NewGuid();
    Name = name.Trim();
  }

  public Guid Id { get; private set; }

  public string Name { get; private set; } = string.Empty;
}
