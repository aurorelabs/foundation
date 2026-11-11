using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Humanizer;

namespace Aurore.Foundation.Core.Extensions;

/// <summary>Text helpers: checks, comparison and case conversion.</summary>
public static class StringExtensions
{
    /// <summary>True when the text is not null, empty or whitespace.</summary>
    /// <param name="value">The text to check.</param>
    public static bool HasValue([NotNullWhen(true)] this string? value)
    {
        return string.IsNullOrWhiteSpace(value) is false;
    }

    /// <summary>Removes accents, so "Ação" becomes "Acao". Null becomes an empty string.</summary>
    public static string RemoveDiacritics(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Case-insensitive, culture-independent comparison.</summary>
    /// <param name="value">The text to compare.</param>
    /// <param name="target">The text to compare with.</param>
    public static bool IsEqualTo(this string? value, string? target)
    {
        return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Words are runs of letters and digits; case changes also split: "ApiService" -&gt; ["Api", "Service"],
    /// "HTTPServer" -&gt; ["HTTP", "Server"], "Version2Api" -&gt; ["Version2", "Api"]. Brand names split too ("GitHub" -&gt; "Git", "Hub").
    /// </summary>
    public static string[] SplitWords(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var words = new List<string>();
        var start = -1;

        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsLetterOrDigit(value[i]))
            {
                if (start < 0)
                {
                    start = i;
                }
                else if (IsCaseBoundary(value, i))
                {
                    words.Add(value[start..i]);
                    start = i;
                }

                continue;
            }

            if (start >= 0)
            {
                words.Add(value[start..i]);
                start = -1;
            }
        }

        if (start >= 0)
            words.Add(value[start..]);

        return [.. words];
    }

    private static bool IsCaseBoundary(string value, int index)
    {
        if (char.IsUpper(value[index]) is false)
            return false;

        var previous = value[index - 1];

        if (char.IsLower(previous) || char.IsDigit(previous))
            return true;

        return char.IsUpper(previous) && index + 1 < value.Length && char.IsLower(value[index + 1]);
    }

    /// <summary>
    /// Splits the text into words and writes it as a sentence: only the first word is capitalized and acronyms are kept
    /// ("ApiService" becomes "Api service", "HTTPServer" becomes "HTTP server"). Uses Humanizer. Null becomes an empty string.
    /// </summary>
    public static string ToSentenceCase(this string? value)
    {
        return value is null ? string.Empty : value.Humanize(LetterCasing.Sentence);
    }

    /// <summary>
    /// Splits the text into words and capitalizes each, keeping acronyms ("apiService" becomes "Api Service", "HTTPServer"
    /// becomes "HTTP Server"). Uses Humanizer. Null becomes an empty string.
    /// </summary>
    public static string ToTitleCase(this string? value)
    {
        return value is null ? string.Empty : value.Titleize();
    }

    /// <summary>The plural of an English word or phrase, irregular ones included ("person" becomes "people"). Null becomes an empty string.</summary>
    public static string ToPlural(this string? value)
    {
        return value is null ? string.Empty : value.Pluralize();
    }

    /// <summary>The singular of an English word or phrase ("people" becomes "person"). Null becomes an empty string.</summary>
    public static string ToSingular(this string? value)
    {
        return value is null ? string.Empty : value.Singularize();
    }

    /// <summary>
    /// Every word capitalized and joined without separators ("api service" becomes "ApiService"). Uses Humanizer. Null
    /// becomes an empty string.
    /// </summary>
    public static string ToPascalCase(this string? value)
    {
        return value is null ? string.Empty : value.Pascalize();
    }

    /// <summary>
    /// First word lower case, the others capitalized, the way System.Text.Json writes camel case: "PageSize" becomes
    /// "pageSize". Null becomes an empty string.
    /// </summary>
    public static string ToCamelCase(this string? value)
    {
        return value is null ? string.Empty : JsonNamingPolicy.CamelCase.ConvertName(value);
    }

    /// <summary>
    /// Words joined with underscores, without accents, lower case ("ApiService" becomes "api_service") or upper case
    /// ("API_SERVICE"). Uses the System.Text.Json snake case policy. Null becomes an empty string.
    /// </summary>
    /// <param name="value">The text to convert.</param>
    /// <param name="lowercase">True for "api_service", false for "API_SERVICE".</param>
    public static string ToSnakeCase(this string? value, bool lowercase = true)
    {
        var policy = lowercase ? JsonNamingPolicy.SnakeCaseLower : JsonNamingPolicy.SnakeCaseUpper;

        return policy.ConvertName(value.RemoveDiacritics());
    }

    /// <summary>
    /// Words joined with hyphens, without accents, lower case ("ApiService" becomes "api-service") or upper case
    /// ("API-SERVICE"). Uses the System.Text.Json kebab case policy. Null becomes an empty string.
    /// </summary>
    /// <param name="value">The text to convert.</param>
    /// <param name="lowercase">True for "api-service", false for "API-SERVICE".</param>
    public static string ToSlugCase(this string? value, bool lowercase = true)
    {
        var policy = lowercase ? JsonNamingPolicy.KebabCaseLower : JsonNamingPolicy.KebabCaseUpper;

        return policy.ConvertName(value.RemoveDiacritics());
    }
}
