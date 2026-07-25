using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TrainingManagement.Application.Common;

public static partial class SlugGenerator
{
    public static string Generate(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        var slug = InvalidCharacters().Replace(builder.ToString().Normalize(NormalizationForm.FormC), "-");
        return RepeatedHyphens().Replace(slug, "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidCharacters();
    [GeneratedRegex("-{2,}")]
    private static partial Regex RepeatedHyphens();
}
