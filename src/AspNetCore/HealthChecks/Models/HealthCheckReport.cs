using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Models;

/// <summary>The answer of the readiness and status endpoints.</summary>
public sealed record HealthCheckReport
{
    /// <summary>The overall status.</summary>
    public required HealthStatus Status { get; init; }
    /// <summary>How long all the checks took together.</summary>
    public required TimeSpan TotalDuration { get; init; }
    /// <summary>When the report was made (UTC).</summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>The dependencies with their details. Left out of the anonymous readiness answer.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<DependencyReport>? Dependencies { get; init; }

    /// <summary>Builds the report, with or without the dependency details.</summary>
    /// <param name="report">The report of the health checks.</param>
    /// <param name="includeDependencies">True to list each dependency; false for the overall status only.</param>
    /// <param name="time">The clock that tells when the report was made.</param>
    /// <returns>The report to send to the caller.</returns>
    public static HealthCheckReport From(HealthReport report, bool includeDependencies, TimeProvider time)
    {
        return new()
        {
            Status = report.Status,
            TotalDuration = report.TotalDuration,
            Timestamp = time.GetUtcNow().UtcDateTime,
            Dependencies = includeDependencies ? report.Entries.Select(DependencyReport.From).ToList() : null,
        };
    }
}
