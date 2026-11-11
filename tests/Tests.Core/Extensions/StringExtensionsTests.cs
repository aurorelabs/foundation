using System;
using Aurore.Foundation.Core.Extensions;

namespace Tests.Core.Extensions;

[Trait("Feature", "String helpers")]
public sealed class StringExtensionsTests
{
    public static TheoryData<Func<string?, string>> NullToEmpty => new()
    {
        x => x.ToTitleCase(),
        x => x.ToSentenceCase(),
        x => x.ToPascalCase(),
        x => x.ToCamelCase(),
        x => x.ToSnakeCase(),
        x => x.ToSlugCase(),
        x => x.ToPlural(),
        x => x.ToSingular(),
        x => x.RemoveDiacritics(),
    };

    public static TheoryData<string?, string[]> Words => new()
    {
        { "ApiService", ["Api", "Service"] },
        { "apiService", ["api", "Service"] },
        { "HTTPServer", ["HTTP", "Server"] },
        { "Version2Api", ["Version2", "Api"] },
        { "api-service_name.v2", ["api", "service", "name", "v2"] },
        { "  padded  text ", ["padded", "text"] },
        { "ação de usuário", ["ação", "de", "usuário"] },
        { "", [] },
        { "   ", [] },
        { null, [] },
    };

    public static TheoryData<string, string> SnakeCases => new()
    {
        { "ApiService", "api_service" },
        { "PageSize2", "page_size2" },
        { "HTTPServer", "http_server" },
        { "Ação de Usuário", "acao_de_usuario" },
    };

    public static TheoryData<string, string> SlugCases => new()
    {
        { "ApiService", "api-service" },
        { "HTTPServer", "http-server" },
        { "Ação de Usuário", "acao-de-usuario" },
    };

    [Theory(DisplayName = "A null text becomes an empty string in every conversion")]
    [MemberData(nameof(NullToEmpty))]
    public void NullBecomesEmpty(Func<string?, string> convert)
    {
        // Arrange
        string? value = null;

        // Act
        var converted = convert(value);

        // Assert
        Assert.Equal(string.Empty, converted);
    }

    [Theory(DisplayName = "Words are split on separators and on case changes, acronyms included")]
    [MemberData(nameof(Words))]
    public void WordsAreSplit(string? value, string[] expected)
    {
        // Arrange
        // Act
        var words = value.SplitWords();

        // Assert
        Assert.Equal(expected, words);
    }

    [Theory(DisplayName = "Snake case drops accents and lowers the text, or upper cases it on request")]
    [MemberData(nameof(SnakeCases))]
    public void SnakeCaseDropsAccents(string value, string expected)
    {
        // Arrange
        // Act
        var lower = value.ToSnakeCase();
        var upper = value.ToSnakeCase(lowercase: false);

        // Assert
        Assert.Equal(expected, lower);
        Assert.Equal(expected.ToUpperInvariant(), upper);
    }

    [Theory(DisplayName = "Slug case drops accents and lowers the text, or upper cases it on request")]
    [MemberData(nameof(SlugCases))]
    public void SlugCaseDropsAccents(string value, string expected)
    {
        // Arrange
        // Act
        var lower = value.ToSlugCase();
        var upper = value.ToSlugCase(lowercase: false);

        // Assert
        Assert.Equal(expected, lower);
        Assert.Equal(expected.ToUpperInvariant(), upper);
    }

    [Fact(DisplayName = "Accents are removed and the letters stay")]
    public void AccentsAreRemoved()
    {
        // Arrange
        // Act
        var plain = "Ação, coração e café".RemoveDiacritics();

        // Assert
        Assert.Equal("Acao, coracao e cafe", plain);
    }
}
