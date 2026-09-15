using System.Globalization;
using System.Net;

namespace MobileCatalogAPI.Services;

public sealed class WhatsAppLinkService(IConfiguration configuration)
{
    public string CreateProductOrderUrl(string phone, string productName, string? variant, decimal price)
    {
        var normalizedPhone = new string(phone.Where(char.IsDigit).ToArray());
        if (normalizedPhone.Length < 8)
            throw new ArgumentException("El teléfono debe incluir el código de país.", nameof(phone));

        var message = $"Hola, quiero consultar/pedir: {productName}."
            + (string.IsNullOrWhiteSpace(variant) ? string.Empty : $" Variante: {variant}.")
            + $" Precio: {price.ToString("N0", CultureInfo.GetCultureInfo("es-PY"))} Gs.";
        var baseUrl = configuration["WhatsApp:BaseUrl"] ?? "https://wa.me";
        return $"{baseUrl.TrimEnd('/')}/{normalizedPhone}?text={WebUtility.UrlEncode(message)}";
    }
}