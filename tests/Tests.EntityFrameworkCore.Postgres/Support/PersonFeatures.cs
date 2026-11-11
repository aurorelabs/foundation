using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Features;
using Aurore.Foundation.Core.Results;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

namespace Tests.EntityFrameworkCore.Postgres.Support;

public sealed record CreatePersonRequest(string Name, string Email);

/// <summary>Adds a person and saves. The decorator is enabled for it by the registration, not by the feature.</summary>
public sealed class CreatePerson(TestDbContext db) : Feature<CreatePersonRequest, Guid>
{
    protected override async ValueTask<Result<Guid>> ExecuteAsync(CreatePersonRequest request, CancellationToken cancellationToken)
    {
        var person = new Person { Name = request.Name, Email = request.Email };

        db.People.Add(person);

        await db.SaveChangesAsync(cancellationToken);

        return person.Id;
    }
}

/// <summary>The same logic, but it enables the decorator itself with the attribute.</summary>
[WithUniqueViolation]
public sealed class CreatePersonMarked(TestDbContext db) : Feature<CreatePersonRequest, Guid>
{
    protected override async ValueTask<Result<Guid>> ExecuteAsync(CreatePersonRequest request, CancellationToken cancellationToken)
    {
        var person = new Person { Name = request.Name, Email = request.Email };

        db.People.Add(person);

        await db.SaveChangesAsync(cancellationToken);

        return person.Id;
    }
}
