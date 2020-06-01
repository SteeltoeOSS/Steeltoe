// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Management.Endpoint
{
    public static class StringExtensions
    {
        /// <summary>
        /// Evaluate if a path starts with a given segment, including optional base paths
        /// </summary>
        /// <param name="incoming">path to evaluate</param>
        /// <param name="other">path to search for</param>
        /// <param name="baseSegments">base segments that should be considered separately</param>
        /// <param name="remaining">the result</param>
        /// <returns>remainder of path after base segment(s) and searched-for segment have been considered</returns>
        public static bool StartsWithSegments(this string incoming, string other, IEnumerable<string> baseSegments, out string remaining)
        {
            var value1 = incoming ?? string.Empty;
            var value2 = other ?? string.Empty;
            if (value1.StartsWith(value2, StringComparison.OrdinalIgnoreCase) && (value1.Length == value2.Length || value1[value2.Length] == '/'))
            {
                remaining = value1.Substring(value2.Length);
                return true;
            }

            if (baseSegments?.Any() == true)
            {
                foreach (var pathBase in baseSegments.Distinct())
                {
                    var testPath = string.Concat(pathBase, '/', value2);
                    if (value1.StartsWith(testPath, StringComparison.OrdinalIgnoreCase) && (value1.Length == testPath.Length || value1[testPath.Length] == '/'))
                    {
                        remaining = value1.Substring(testPath.Length);
                        return true;
                    }
                }
            }

            remaining = string.Empty;
            return false;
        }
    }
}
