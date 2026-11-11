using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.AspNetCore.HealthChecks.Checks;

/// <summary>Calls an upstream HTTP service and reports whether it answered with a success status.</summary>
public sealed class HttpDependencyHealthCheck(ILogger<HttpDependencyHealthCheck> logger, IHttpClientFactory clientFactory) : IHealthCheck
{
    private const string ClientNamePrefix = "health-check-";

    /// <summary>The name of the HTTP client used to check the named dependency.</summary>
    /// <param name="checkName">The name of the health check.</param>
    /// <returns>The name of the HttpClient the check uses.</returns>
    public static string GetClientName(string checkName)
    {
        return $"{ClientNamePrefix}{checkName}";
    }

    /// <summary>
    /// Calls the dependency. Success is healthy; any other status or a failure to connect gives the check's failure
    /// status.
    /// </summary>
    /// <param name="context">The health check registration being run.</param>
    /// <param name="cancellationToken">A token to cancel the check.</param>
    /// <returns>Healthy when the dependency answers with a success status; otherwise the failure status of the registration.</returns>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var name = context.Registration.Name;
        var failureStatus = context.Registration.FailureStatus;

        try
        {
            var client = clientFactory.CreateClient(GetClientName(name));

            using var response = await client.GetAsync(string.Empty, cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy();

            logger.LogWarning("Health check for dependency {DependencyName} returned status code {StatusCode}.", name, (int)response.StatusCode);

            return new HealthCheckResult(failureStatus, $"Dependency returned status code {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check for dependency {DependencyName} could not reach the dependency.", name);

            return new HealthCheckResult(failureStatus, "Dependency could not be reached.", ex);
        }
    }
}
