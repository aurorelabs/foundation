using System;
using System.Collections.Generic;
using System.Reflection;
using Aurore.Foundation.Core.Validation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Aurore.Foundation.AspNetCore.Configuration;

/// <summary>Registers the validators of the application.</summary>
public static class ValidatorConfiguration
{
    /// <summary>
    /// Scans the assemblies for validators and registers the two kinds that run automatically: those deriving from
    /// RequestValidator as <see cref="IRequestValidator{T}"/> (run by the HTTP validation filter) and those deriving from
    /// FeatureValidator as <see cref="IFeatureValidator{T}"/> (run by the feature pipeline). Any other validator, such as a
    /// plain AbstractValidator used for settings, is not registered: create it by hand where it is needed.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="includeInternalTypes">True to find validators that are not public, which is how most are declared.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddAuroreValidators(this IServiceCollection services, IEnumerable<Assembly> assemblies, bool includeInternalTypes = true)
    {
        foreach (var result in AssemblyScanner.FindValidatorsInAssemblies(assemblies, includeInternalTypes))
        {
            AddMarker(services, result, typeof(IRequestValidator<>));
            AddMarker(services, result, typeof(IFeatureValidator<>));
        }

        return services;
    }

    private static void AddMarker(IServiceCollection services, AssemblyScanner.AssemblyScanResult result, Type marker)
    {
        var markerType = marker.MakeGenericType(result.InterfaceType.GetGenericArguments()[0]);

        if (markerType.IsAssignableFrom(result.ValidatorType))
        {
            services.TryAddEnumerable(ServiceDescriptor.Scoped(markerType, result.ValidatorType));
        }
    }
}
