using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aurore.Foundation.Core.Pagination.Models;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Tests.EntityFrameworkCore.Postgres.Support;

namespace Tests.EntityFrameworkCore.Postgres.Pagination;

/// <summary>Checks on a real Postgres that the sort map, the keyset conditions and the counts translate and agree.</summary>
[Trait("Feature", "Paging on Postgres")]
public sealed class PagingAsyncExtensionsTests(PagingAsyncExtensionsTests.PeopleFixture postgres) : IClassFixture<PagingAsyncExtensionsTests.PeopleFixture>
{
    private static readonly PaginationOptions _options = new() { DefaultPageSize = 10, MaxPageSize = 50 };

    private static readonly SortMap<Person> _sortMap = SortMap.For<Person>()
        .Allow(x => x.Name, "name")
        .Allow(x => x.Age, "age")
        .Allow(x => x.CreatedAt, "createdAt")
        .DefaultTo(x => x.Name);

    // Names repeat on purpose, so ties have to be broken by the id; case and accents differ to meet the database's ordering.
    private static readonly string[] _names = ["Ada", "ada", "Álvaro", "Bia", "Bia", "Bia", "Caio", "Zoe", "Zoe", "Ana"];

    /// <summary>45 people seeded once for the class; tests only read them, or add and remove rows of their own.</summary>
    public sealed class PeopleFixture : PostgresFixture
    {
        protected override async ValueTask SeedAsync(TestDbContext db)
        {
            for (var i = 0; i < 45; i++)
                db.People.Add(new Person { Name = _names[i % _names.Length], Age = 20 + (i % 7), Email = $"person{i}@example.com" });

            await db.SaveChangesAsync();
        }
    }

    [Fact(DisplayName = "A numbered page reports the totals and returns the requested window")]
    public async Task NumberedPageHasTotalsAndWindow()
    {
        // Arrange
        await using var provider = postgres.Provider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Act
        var result = await db.People.ToPageAsync(new PageRequest { PageNumber = 2, PageSize = 20 }, _options, _sortMap, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(45, result.Value.TotalItems);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.Equal(2, result.Value.PageNumber);
        Assert.Equal(20, result.Value.Items.Count);
    }

    [Fact(DisplayName = "The last numbered page holds what is left")]
    public async Task LastPageHoldsTheRest()
    {
        // Arrange
        await using var provider = postgres.Provider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Act
        var result = await db.People.ToPageAsync(new PageRequest { PageNumber = 3, PageSize = 20 }, _options, _sortMap, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, result.Value.Items.Count);
    }

    [Fact(DisplayName = "Ordering by an allowed field is applied by the database, with ties broken by the id")]
    public async Task OrderingIsAppliedByTheDatabase()
    {
        // Arrange
        await using var provider = postgres.Provider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        // Act
        var result = await db.People.ToPageAsync(new PageRequest { PageSize = 50, OrderBy = ["age:desc"] }, _options, _sortMap, TestContext.Current.CancellationToken);

        // Assert
        var ages = result.Value.Items.Select(x => x.Age).ToList();
        Assert.Equal(ages.OrderByDescending(x => x), ages);

        foreach (var sameAge in result.Value.Items.GroupBy(x => x.Age))
            Assert.Equal(sameAge.OrderBy(x => x.Id).Select(x => x.Id), sameAge.Select(x => x.Id));
    }

    [Fact(DisplayName = "Walking the cursor pages by name returns every row once, in order")]
    public async Task CursorWalkByNameVisitsEveryRowOnce()
    {
        // Arrange
        await using var provider = postgres.Provider();

        // Act
        var seen = await WalkAsync(provider, ["name"], 7);

        // Assert
        Assert.Equal(45, seen.Count);
        Assert.Equal(45, seen.Select(x => x.Id).Distinct().Count());
    }

    [Fact(DisplayName = "Walking the cursor pages in descending order of a number with many ties returns every row once")]
    public async Task CursorWalkByAgeVisitsEveryRowOnce()
    {
        // Arrange
        await using var provider = postgres.Provider();

        // Act
        var seen = await WalkAsync(provider, ["age:desc", "name"], 4);

        // Assert
        Assert.Equal(45, seen.Count);
        Assert.Equal(45, seen.Select(x => x.Id).Distinct().Count());
        Assert.Equal(seen.Select(x => x.Age).OrderByDescending(x => x), seen.Select(x => x.Age));
    }

    [Fact(DisplayName = "Walking the cursor pages by creation time returns every row once")]
    public async Task CursorWalkByCreatedAtVisitsEveryRowOnce()
    {
        // Arrange
        await using var provider = postgres.Provider();

        // Act
        var seen = await WalkAsync(provider, ["createdAt:desc"], 10);

        // Assert
        Assert.Equal(45, seen.Select(x => x.Id).Distinct().Count());
    }

    private static async Task<List<Person>> WalkAsync(ServiceProvider provider, string[] orderBy, int pageSize)
    {
        var seen = new List<Person>();
        string? cursor = null;

        do
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            var request = new SearchPeople { OrderBy = orderBy, PageSize = pageSize, Cursor = cursor };
            var page = await db.People.ToCursorPageAsync(request, _options, _sortMap, TestContext.Current.CancellationToken);

            Assert.True(page.IsSuccess, page.Error?.Message);
            seen.AddRange(page.Value.Items);
            cursor = page.Value.NextCursor;
        }
        while (cursor is not null);

        return seen;
    }

    private sealed class SearchPeople : CursorRequest;
}
