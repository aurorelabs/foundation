using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Aurore.Foundation.AspNetCore.Authentication.Extensions;
using Aurore.Foundation.Core.Constants;
using Aurore.Foundation.Core.Models;
using Aurore.Foundation.Core.Privacy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aurore.Foundation.AspNetCore.RequestPipeline.Middlewares;

/// <summary>
/// Runs after authentication. Puts the user id in the request context and, encrypted when privacy is on, in the log scope
/// and on the current span (the span tag is protected again on export by the privacy trace processor).
/// </summary>
public sealed class RequestUserMiddleware(RequestDelegate next, IPrivacyProtector privacy, ILogger<RequestUserMiddleware> logger)
{
    /// <summary>Adds the authenticated user's id to the context, the span and the log scope.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="requestContext">The context of the request that receives the user id.</param>
    public async Task InvokeAsync(HttpContext context, RequestContext requestContext)
    {
        var userId = context.User.GetUserId();

        if (userId is null)
        {
            await next(context);

            return;
        }

        requestContext.SetUserId(userId);

        Activity.Current?.SetTag(OpenTelemetryProperties.Attributes.Standard.EndUserId, userId);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            [OpenTelemetryProperties.Attributes.Standard.EndUserId] = privacy.Protect(OpenTelemetryProperties.Attributes.Standard.EndUserId, userId),
        });

        await next(context);
    }
}
