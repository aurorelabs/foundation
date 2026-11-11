using Microsoft.AspNetCore.Http;

namespace Aurore.Foundation.AspNetCore.Extensions;

/// <summary>Helpers for reading the current request.</summary>
public static class HttpContextExtensions
{
    /// <summary>Returns the request header value, or the fallback (empty by default) when the header is absent.</summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="key">The name of the header.</param>
    /// <param name="fallbackValue">What to return when the header is absent. Defaults to an empty string.</param>
    /// <returns>The value of the header, or the fallback.</returns>
    public static string GetRequestHeader(this HttpContext context, string key, string? fallbackValue = null)
    {
        return context.Request.Headers.TryGetValue(key, out var headerValue) is not true
            ? fallbackValue ?? string.Empty
            : headerValue.ToString();
    }
}
