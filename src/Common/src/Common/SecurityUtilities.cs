// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Steeltoe.Common
{
    /// <summary>
    /// Helpers for compliance with security scans
    /// </summary>
    public static class SecurityUtilities
    {
        /// <summary>
        /// Remove line endings and HTML-encode strings. Useful for logging (potentially) user-entered data
        /// </summary>
        /// <param name="inputString">Some string to sanitize</param>
        /// <returns>HTML-encoded version of original string with CR and LF removed</returns>
        public static string SanitizeInput(string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
            {
                return inputString;
            }

            return WebUtility.HtmlEncode(inputString.Replace("\r", string.Empty).Replace("\n", string.Empty));
        }
    }
}
