using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace MobileCatalogAPI.Services;

public sealed class CloudinaryImageService
{
    private readonly Cloudinary? cloudinary;

    public CloudinaryImageService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];
        if (!string.IsNullOrWhiteSpace(cloudName) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiSecret))
            cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
    }

    public async Task<string> UploadAsync(IFormFile file)
    {
        if (cloudinary is null)
            throw new InvalidOperationException("Cloudinary no está configurado.");
        if (file is null || file.Length is <= 0 or > 10 * 1024 * 1024)
            throw new ArgumentException("La imagen debe pesar entre 1 byte y 10 MB.", nameof(file));
        if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("El archivo debe ser una imagen.", nameof(file));

        await using var stream = file.OpenReadStream();
        var result = await cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "mobile-catalog"
        });
        if (result.Error is not null)
            throw new InvalidOperationException(result.Error.Message);
        return result.SecureUrl.ToString();
    }
}