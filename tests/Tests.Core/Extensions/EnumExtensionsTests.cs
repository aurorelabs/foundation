using System;
using Aurore.Foundation.Core.Extensions;

namespace Tests.Core.Extensions;

[Trait("Feature", "Enum parsing")]
public sealed class EnumExtensionsTests
{
    public static TheoryData<string> NotMembers => new()
    {
        "",
        "purple",
        "99", // a number that parses but is not a member
        "Red,Green", // a combination that parses but is not a member
    };

    [Theory(DisplayName = "A name is parsed whatever its casing")]
    [InlineData("Green")]
    [InlineData("green")]
    [InlineData("GREEN")]
    public void NameIsParsedWhateverItsCasing(string value)
    {
        // Arrange
        // Act
        var parsed = value.ParseEnum<Colour>();

        // Assert
        Assert.Equal(Colour.Green, parsed);
    }

    [Theory(DisplayName = "Parsing something that is not a member throws and lists the members")]
    [MemberData(nameof(NotMembers))]
    public void ParsingANonMemberThrows(string value)
    {
        // Arrange
        // Act
        var thrown = Assert.Throws<InvalidOperationException>(() => value.ParseEnum<Colour>());

        // Assert
        Assert.Contains("Red, Green", thrown.Message);
    }

    [Theory(DisplayName = "Parsing with a fallback returns the fallback for anything that is not a member")]
    [MemberData(nameof(NotMembers))]
    public void FallbackIsUsedForANonMember(string value)
    {
        // Arrange
        // Act
        var parsed = value.ParseEnumOrDefault(Colour.Red);

        // Assert
        Assert.Equal(Colour.Red, parsed);
    }

    [Fact(DisplayName = "A missing (null) text is not a member: parsing throws and the fallback is used")]
    public void NullIsNotAMember()
    {
        // Arrange
        string? value = null;

        // Act
        var thrown = Record.Exception(() => value.ParseEnum<Colour>());
        var parsed = value.ParseEnumOrDefault(Colour.Red);

        // Assert
        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal(Colour.Red, parsed);
    }

    [Fact(DisplayName = "Parsing with a fallback returns the member when there is one")]
    public void MemberWinsOverTheFallback()
    {
        // Arrange
        // Act
        var parsed = "green".ParseEnumOrDefault(Colour.Red);

        // Assert
        Assert.Equal(Colour.Green, parsed);
    }

    private enum Colour
    {
        Red = 1,
        Green = 2,
    }
}
