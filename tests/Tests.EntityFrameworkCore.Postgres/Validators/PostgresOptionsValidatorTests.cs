using System.Collections.Generic;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Validators;
using Microsoft.Extensions.Configuration;

namespace Tests.EntityFrameworkCore.Postgres.Validators;

[Trait("Feature", "Postgres settings")]
public sealed class PostgresOptionsValidatorTests
{
    public static TheoryData<string> RequiredKeywords => new() { "Host", "Database", "Username", "Password" };

    public static TheoryData<string, string> InvalidSettings => new()
    {
        { "Colour", "red" }, // not a keyword Npgsql knows
        { "Port", "not-a-number" }, // a value of the wrong type
    };

    [Fact(DisplayName = "Complete settings are valid")]
    public void CompleteSettingsAreValid()
    {
        // Arrange
        var validator = new PostgresOptionsValidator(isProduction: false);

        // Act
        var result = validator.Validate(Complete());

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory(DisplayName = "A missing required keyword is reported by name")]
    [MemberData(nameof(RequiredKeywords))]
    public void MissingKeywordIsReported(string keyword)
    {
        // Arrange
        var validator = new PostgresOptionsValidator(isProduction: false);
        var options = Complete();
        options.Remove(keyword);

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Contains(result.Errors, x => x.PropertyName == keyword);
    }

    [Theory(DisplayName = "An unknown keyword or a value of the wrong type is rejected")]
    [MemberData(nameof(InvalidSettings))]
    public void InvalidSettingsAreRejected(string keyword, string value)
    {
        // Arrange
        var validator = new PostgresOptionsValidator(isProduction: false);
        var options = Complete();
        options[keyword] = value;

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.False(result.IsValid);
    }

    [Theory(DisplayName = "A local host is accepted outside production and rejected in production")]
    [InlineData("localhost", false, true)]
    [InlineData("127.0.0.1", false, true)]
    [InlineData("localhost", true, false)]
    [InlineData("127.0.0.1", true, false)]
    [InlineData("db.internal.example.com", true, true)]
    public void LocalHostIsRejectedInProduction(string host, bool isProduction, bool expectedValid)
    {
        // Arrange
        var validator = new PostgresOptionsValidator(isProduction);
        var options = Complete();
        options["Host"] = host;

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact(DisplayName = "Keywords are not case sensitive")]
    public void KeywordsAreNotCaseSensitive()
    {
        // Arrange
        var validator = new PostgresOptionsValidator(isProduction: false);
        var options = new PostgresOptions { ["host"] = "localhost", ["DATABASE"] = "app", ["username"] = "app", ["PassWord"] = "secret" };

        // Act
        var result = validator.Validate(options);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "The settings bind from a configuration section as a free-form map")]
    public void SettingsBindFromConfiguration()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Postgres:Host"] = "db",
                ["Postgres:SSL Mode"] = "Require",
            })
            .Build();

        // Act
        var options = configuration.GetSection("Postgres").Get<PostgresOptions>();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("db", options["host"]);
        Assert.Equal("Require", options["SSL Mode"]);
    }

    private static PostgresOptions Complete()
    {
        return new PostgresOptions { ["Host"] = "localhost", ["Database"] = "app", ["Username"] = "app", ["Password"] = "secret" };
    }
}
