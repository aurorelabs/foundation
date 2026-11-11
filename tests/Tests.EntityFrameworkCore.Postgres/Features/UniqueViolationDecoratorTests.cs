using System.Threading.Tasks;
using Aurore.Foundation.Core.Errors;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Errors;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.EntityFrameworkCore.Postgres.Support;

namespace Tests.EntityFrameworkCore.Postgres.Features;

[Trait("Feature", "Unique violation")]
public sealed class UniqueViolationDecoratorTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    private static readonly ApplicationError _emailTaken = ApplicationError.Conflict("person.email_taken", "That email is already registered.");

    [Fact(DisplayName = "A feature that saves a free value succeeds")]
    public async Task FreeValueSucceeds()
    {
        // Arrange
        await using var provider = postgres.Provider(features => features.WithUniqueViolation());
        await using var scope = provider.CreateAsyncScope();
        var feature = scope.ServiceProvider.GetRequiredService<CreatePerson>();

        // Act
        var result = await feature.HandleAsync(new CreatePersonRequest("Ada", "free@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "A duplicate with no registered error answers with the generic conflict and the violated field, without the constraint name")]
    public async Task DuplicateAnswersWithTheGenericConflictAndTheField()
    {
        // Arrange
        await using var provider = postgres.Provider(features => features.WithUniqueViolation());
        await CreateAsync(provider, "duplicate@example.com");

        // Act
        var result = await CreateAsync(provider, "duplicate@example.com");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(DatabaseErrors.Conflict.Code, result.Error!.Code);
        Assert.Equal(["email"], result.Error.Details.Keys);
        Assert.Equal(ErrorKind.Conflict, result.Error!.Kind);
        Assert.DoesNotContain(PersonConfiguration.UniqueEmailIndex, result.Error.Message);
    }

    [Fact(DisplayName = "A duplicate answers with the error registered for its constraint")]
    public async Task DuplicateAnswersWithTheRegisteredError()
    {
        // Arrange
        await using var provider = postgres.Provider(
            features => features.WithUniqueViolation(),
            services => services.AddUniqueViolationErrors(map => map.On(PersonConfiguration.UniqueEmailIndex, _emailTaken)));
        await CreateAsync(provider, "registered@example.com");

        // Act
        var result = await CreateAsync(provider, "registered@example.com");

        // Assert
        Assert.Equal(_emailTaken, result.Error);
    }

    [Fact(DisplayName = "The attribute on a feature enables the decorator without the registration")]
    public async Task AttributeEnablesTheDecorator()
    {
        // Arrange
        await using var provider = postgres.Provider();

        await using (var scope = provider.CreateAsyncScope())
        {
            var first = scope.ServiceProvider.GetRequiredService<CreatePersonMarked>();
            await first.HandleAsync(new CreatePersonRequest("Ada", "marked@example.com"), TestContext.Current.CancellationToken);
        }

        // Act
        await using var second = provider.CreateAsyncScope();
        var result = await second.ServiceProvider.GetRequiredService<CreatePersonMarked>()
            .HandleAsync(new CreatePersonRequest("Ada", "marked@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(DatabaseErrors.Conflict.Code, result.Error!.Code);
    }

    [Fact(DisplayName = "Another database failure is not hidden as a conflict")]
    public async Task OtherDatabaseErrorsEscape()
    {
        // Arrange
        await using var provider = postgres.Provider(features => features.WithUniqueViolation());
        var tooLong = new string('x', 200);

        // Act
        var thrown = await Record.ExceptionAsync(async () => await CreateAsync(provider, "long@example.com", tooLong));

        // Assert
        Assert.IsType<DbUpdateException>(thrown);
    }

    private static async Task<Aurore.Foundation.Core.Results.Result<System.Guid>> CreateAsync(ServiceProvider provider, string email, string name = "Ada")
    {
        await using var scope = provider.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<CreatePerson>()
            .HandleAsync(new CreatePersonRequest(name, email), TestContext.Current.CancellationToken);
    }
}
