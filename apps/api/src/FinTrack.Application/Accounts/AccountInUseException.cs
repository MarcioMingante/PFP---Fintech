namespace FinTrack.Application.Accounts;

public sealed class AccountInUseException : InvalidOperationException
{
    public AccountInUseException(Exception innerException)
        : base("Conta com transações ou recorrências não pode ser excluída.", innerException) { }
}
