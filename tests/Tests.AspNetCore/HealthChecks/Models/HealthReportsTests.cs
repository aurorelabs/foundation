using System;
using System.Collections.Generic;
using Aurore.Foundation.AspNetCore.HealthChecks.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Time.Testing;

namespace Tests.AspNetCore.HealthChecks.Models;

[Trait("Feature", "Health reports")]
public sealed class HealthReportsTests
{
    [Fact(DisplayName = "The liveness answer is stamped by the clock it is given")]
    public void LivenessUsesTheClock()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 30, 0, TimeSpan.Zero));

        // Act
        var report = LivenessReport.Create("billing", "1.2.3", time);

        // Assert
        Assert.Equal(new DateTime(2026, 11, 11, 10, 30, 0, DateTimeKind.Utc), report.Timestamp);
        Assert.Equal("billing", report.Name);
        Assert.Equal("1.2.3", report.Version);
    }

    [Fact(DisplayName = "The health report is stamped by the clock it is given, and lists the dependencies only when asked")]
    public void HealthReportUsesTheClock()
    {
        // Arrange
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 10, 30, 0, TimeSpan.Zero));
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["db"] = new(HealthStatus.Healthy, "ok", TimeSpan.FromMilliseconds(5), null, null),
        };
        var source = new HealthReport(entries, TimeSpan.FromMilliseconds(5));

        // Act
        var summary = HealthCheckReport.From(source, includeDependencies: false, time);
        var detailed = HealthCheckReport.From(source, includeDependencies: true, time);

        // Assert
        Assert.Equal(new DateTime(2026, 11, 11, 10, 30, 0, DateTimeKind.Utc), summary.Timestamp);
        Assert.Null(summary.Dependencies);
        Assert.Single(detailed.Dependencies!);
    }
}
