using System.Net.Mail;

namespace FinTrack.Application.Authentication;

public static class CredentialsValidator
{
    public static Dictionary<string, string[]> Validate(Credentials credentials)
    {
        var errors = new Dictionary<string, string[]>();
        var email = credentials.Email?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 254 ||
            !MailAddress.TryCreate(email, out var address) || address.Address != email)
            errors["email"] = ["Informe um e-mail válido."];
        if (string.IsNullOrEmpty(credentials.Password) || credentials.Password.Length is < 12 or > 128)
            errors["password"] = ["Senha deve conter entre 12 e 128 caracteres."];
        return errors;
    }
}
