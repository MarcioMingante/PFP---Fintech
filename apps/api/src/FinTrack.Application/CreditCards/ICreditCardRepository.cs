using FinTrack.Domain.CreditCards;

namespace FinTrack.Application.CreditCards;

public interface ICreditCardRepository
{
  Task AddAsync(
    CreditCard creditCard,
    CancellationToken cancellationToken = default
  );

  Task<IReadOnlyList<CreditCard>> ListAsync(
    CancellationToken cancellationToken = default
  );

  Task<CreditCard?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default
  );

  Task UpdateAsync(
    CreditCard creditCard,
    CancellationToken cancellationToken = default
  );

  Task DeleteAsync(
    CreditCard creditCard,
    CancellationToken cancellationToken = default
  );
}
