using System;
using Aurore.Foundation.Core.Features.Attributes;
using Aurore.Foundation.Core.Features.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Features;

/// <summary>Turns on the unique violation decorator and registers its error map.</summary>
public static class UniqueViolationConfiguration
{
    /// <summary>Enables the <see cref="UniqueViolationDecorator"/> for every feature of the registration.</summary>
    public static FeaturesBuilder WithUniqueViolation(this FeaturesBuilder builder)
    {
        return builder.With<UniqueViolationDecorator>();
    }

    /// <summary>Registers which error each unique constraint answers with.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Maps the constraint names to errors.</param>
    public static IServiceCollection AddUniqueViolationErrors(this IServiceCollection services, Action<UniqueViolationMap> configure)
    {
        var map = new UniqueViolationMap();

        configure(map);

        return services.AddSingleton(map);
    }
}

/// <summary>Enables the unique violation decorator for this feature.</summary>
public sealed class WithUniqueViolationAttribute : FeatureDecoratorAttribute
{
    /// <inheritdoc />
    public override Type DecoratorType => typeof(UniqueViolationDecorator);
}
