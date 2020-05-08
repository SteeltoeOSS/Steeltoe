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
