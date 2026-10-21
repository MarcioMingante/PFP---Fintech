using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinTrack.Api.Serialization;

// Decimal JSON strings avoid loss of cents in JavaScript JSON.parse (IEEE-754).
public sealed class ExactDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetDecimal(); // Existing numeric API clients.
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (text is not null && System.Text.RegularExpressions.Regex.IsMatch(text, @"^-?\d+(\.\d{1,2})?$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                && decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
                return value;
        }
        throw new JsonException("Informe valor decimal com até duas casas, usando ponto e sem expoente.");
    }
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("0.00", CultureInfo.InvariantCulture));
}
