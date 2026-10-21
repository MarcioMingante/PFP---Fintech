namespace FinTrack.Domain.CreditCards;

public sealed class CreditCard
{
    private CreditCard() { }

    public CreditCard(string name, decimal limit, int closingDay, int dueDay)
    {
        Update(name, limit, closingDay, dueDay);
        Id = Guid.NewGuid();
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Limit { get; private set; }
    public int ClosingDay { get; private set; }
    public int DueDay { get; private set; }

    public void AssignOwner(Guid userId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("Usuário obrigatório.", nameof(userId));
        if (UserId != Guid.Empty && UserId != userId)
            throw new InvalidOperationException("Proprietário não pode ser alterado.");
        UserId = userId;
    }

    public void Update(string name, decimal limit, int closingDay, int dueDay)
    {
        var errors = CreditCardRules.Validate(name, limit, closingDay, dueDay);
        if (errors.Count > 0) throw new ArgumentException(errors.First().Value[0], errors.First().Key);
        Name = name.Trim();
        Limit = limit;
        ClosingDay = closingDay;
        DueDay = dueDay;
    }
}
