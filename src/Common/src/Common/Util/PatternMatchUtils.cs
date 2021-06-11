// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util
{
    public static class PatternMatchUtils
    {
        public static bool SimpleMatch(string pattern, string str)
        {
            if (pattern == null || str == null)
            {
                return false;
            }

            var firstIndex = pattern.IndexOf('*');
            if (firstIndex == -1)
            {
                return pattern.Equals(str);
            }

            if (firstIndex == 0)
            {
                if (pattern.Length == 1)
                {
                    return true;
                }

                var nextIndex = pattern.IndexOf('*', firstIndex + 1);
                if (nextIndex == -1)
                {
                    return str.EndsWith(pattern.Substring(1));
                }

                var part = pattern.Substring(1, nextIndex - 1);
                if (string.IsNullOrEmpty(part))
                {
                    return SimpleMatch(pattern.Substring(nextIndex), str);
                }

                var partIndex = str.IndexOf(part);
                while (partIndex != -1)
                {
                    if (SimpleMatch(pattern.Substring(nextIndex), str.Substring(partIndex + part.Length)))
                    {
                        return true;
                    }

                    partIndex = str.IndexOf(part, partIndex + 1);
                }

                return false;
            }

            return str.Length >= firstIndex &&
                    pattern.Substring(0, firstIndex).Equals(str.Substring(0, firstIndex)) &&
                    SimpleMatch(pattern.Substring(firstIndex), str.Substring(firstIndex));
        }

        public static bool SimpleMatch(string[] patterns, string str)
        {
            if (patterns != null)
            {
                foreach (var pattern in patterns)
                {
                    if (SimpleMatch(pattern, str))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
