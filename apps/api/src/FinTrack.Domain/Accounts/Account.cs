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

  public void Rename(string name)
  {
      if (string.IsNullOrWhiteSpace(name))
      {
          throw new ArgumentException(
              "Nome da conta não pode ser vazio.",
              nameof(name)
          );
      }

      Name = name.Trim();
  }

  public Guid? UserId { get; private set; }

  public void AssignOwner(Guid userId)
  {
    if (userId == Guid.Empty) throw new ArgumentException("Usuário obrigatório.", nameof(userId));
    if (UserId.HasValue && UserId != userId) throw new InvalidOperationException("Proprietário não pode ser alterado.");
    UserId = userId;
  }

  public Guid Id { get; private set; }

  public string Name { get; private set; } = string.Empty;
}
