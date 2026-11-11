using System;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Models;

/// <summary>The answer of the liveness endpoint: the service is up.</summary>
public sealed record LivenessReport
{
    /// <summary>The service name.</summary>
    public required string Name { get; init; }
    /// <summary>The service version.</summary>
    public required string Version { get; init; }
    /// <summary>When the answer was made (UTC).</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>Creates the report for the given service name and version.</summary>
    /// <param name="name">The name of the application.</param>
    /// <param name="version">The running version.</param>
    /// <param name="time">The clock that tells when the answer was made.</param>
    public static LivenessReport Create(string name, string version, TimeProvider time)
    {
        return new()
        {
            Name = name,
            Version = version,
            Timestamp = time.GetUtcNow().UtcDateTime,
        };
    }
}
