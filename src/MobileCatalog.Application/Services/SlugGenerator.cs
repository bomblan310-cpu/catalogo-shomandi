using System.Globalization;
using System.Text;

namespace MobileCatalog.Application.Services;

public static class SlugGenerator
{
    public static async Task<string> UniqueSlugAsync(IQueryable<string> existingSlugs, string name, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(name);
        var slug = baseSlug;
        var suffix = 1;
        while (existingSlugs.Any(item => item == slug))
            slug = $"{baseSlug}-{++suffix}";
        await Task.CompletedTask;
        return slug;
    }

    public static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }
        var slug = string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
