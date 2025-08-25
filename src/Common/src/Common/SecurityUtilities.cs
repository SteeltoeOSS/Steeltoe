// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Steeltoe.Common;

/// <summary>
/// Helpers for compliance with security scans.
/// </summary>
internal static class SecurityUtilities
{
    /// <summary>
    /// Removes line breaks from text and HTML-encodes it. Useful for logging (potentially) user-entered data.
    /// </summary>
    /// <param name="text">
    /// The text to sanitize.
    /// </param>
    /// <returns>
    /// The HTML-encoded version of the original string, with line breaks removed.
    /// </returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? SanitizeInput(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        return WebUtility.HtmlEncode(text.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal));
    }

    /// <summary>
    /// Validates that a string contains only safe characters for logging.
    /// Removes potentially dangerous characters that could be used for log injection attacks.
    /// </summary>
    /// <param name="text">
    /// The text to validate and sanitize.
    /// </param>
    /// <returns>
    /// A sanitized version of the input suitable for safe logging.
    /// </returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? SanitizeForLogging(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Remove control characters except tab, and limit to printable ASCII
        return new string(text.Where(c => (c >= 32 && c <= 126) || c == '\t').ToArray());
    }

    /// <summary>
    /// Validates that a URL is safe and follows expected patterns.
    /// </summary>
    /// <param name="url">
    /// The URL to validate.
    /// </param>
    /// <returns>
    /// True if the URL is considered safe, false otherwise.
    /// </returns>
    public static bool IsUrlSafe(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        // Only allow HTTP and HTTPS schemes
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
