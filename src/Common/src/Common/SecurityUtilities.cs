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
}
