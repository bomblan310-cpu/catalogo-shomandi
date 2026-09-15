using System.Globalization;
using System.Net;

namespace MobileCatalog.Application.Services;

public sealed class WhatsAppLinkService(string baseUrl)
{
    public string CreateProductOrderUrl(string phone, string productName, string? variant, decimal price)
    {
        var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());
        if (normalizedPhone.Length < 8)
            throw new ArgumentException("El teléfono debe incluir el código de país.", nameof(phone));

        var message = $"Hola, quiero consultar/pedir: {productName}."
            + (string.IsNullOrWhiteSpace(variant) ? string.Empty : $" Variante: {variant}.")
            + $" Precio: {price.ToString("N0", CultureInfo.GetCultureInfo("es-PY"))} Gs.";
        return $"{baseUrl.TrimEnd('/')}/{normalizedPhone}?text={WebUtility.UrlEncode(message)}";
    }
}
