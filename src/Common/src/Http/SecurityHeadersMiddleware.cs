// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Common.Http;

/// <summary>
/// Middleware to add security headers to HTTP responses.
/// </summary>
internal sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(next);
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Add security headers to protect against common attacks
        AddSecurityHeaders(context.Response);

        await _next(context);
    }

    private static void AddSecurityHeaders(HttpResponse response)
    {
        var headers = response.Headers;

        // Prevent MIME type sniffing
        if (!headers.ContainsKey("X-Content-Type-Options"))
        {
            headers.Append("X-Content-Type-Options", "nosniff");
        }

        // Prevent clickjacking attacks
        if (!headers.ContainsKey("X-Frame-Options"))
        {
            headers.Append("X-Frame-Options", "DENY");
        }

        // Enable XSS protection in older browsers
        if (!headers.ContainsKey("X-XSS-Protection"))
        {
            headers.Append("X-XSS-Protection", "1; mode=block");
        }

        // Prevent information disclosure
        if (!headers.ContainsKey("X-Powered-By"))
        {
            headers.Remove("X-Powered-By");
        }

        // Remove server information
        if (!headers.ContainsKey("Server"))
        {
            headers.Remove("Server");
        }

        // Content Security Policy for management endpoints
        if (!headers.ContainsKey("Content-Security-Policy"))
        {
            headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'");
        }

        // Referrer policy to limit information leakage
        if (!headers.ContainsKey("Referrer-Policy"))
        {
            headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        }
    }
}

/// <summary>
/// Extension methods for adding security headers middleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>
    /// Adds security headers middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}