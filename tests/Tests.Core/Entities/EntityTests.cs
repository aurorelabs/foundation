using System.Linq;
using Aurore.Foundation.Core.Entities;

namespace Tests.Core.Entities;

[Trait("Feature", "Entities")]
public sealed class EntityTests
{
    [Fact(DisplayName = "A new entity gets its own time-ordered id before it is saved")]
    public void NewEntityHasAnId()
    {
        // Arrange
        // Act
        var entities = Enumerable.Range(0, 50).Select(_ => new Sample()).ToList();

        // Assert
        Assert.All(entities, x => Assert.Equal(7, x.Id.Version));
        Assert.Equal(50, entities.Select(x => x.Id).Distinct().Count());
    }

    private sealed class Sample : Entity;
}
