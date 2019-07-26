// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
