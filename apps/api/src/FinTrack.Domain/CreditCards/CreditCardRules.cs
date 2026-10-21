namespace FinTrack.Domain.CreditCards;

public static class CreditCardRules
{
    public const decimal MaximumLimit = 9999999999999999.99m;

    public static Dictionary<string, string[]> Validate(string? name, decimal limit, int closingDay, int dueDay)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            errors["name"] = ["Nome do cartão deve conter entre 1 e 100 caracteres."];
        if (limit <= 0 || limit > MaximumLimit || decimal.Round(limit, 2) != limit)
            errors["limit"] = ["Limite deve ser positivo, com até 16 dígitos inteiros e duas casas decimais."];
        if (closingDay is < 1 or > 31)
            errors["closingDay"] = ["Dia de fechamento deve estar entre 1 e 31."];
        if (dueDay is < 1 or > 31)
            errors["dueDay"] = ["Dia de vencimento deve estar entre 1 e 31."];
        return errors;
    }
}
