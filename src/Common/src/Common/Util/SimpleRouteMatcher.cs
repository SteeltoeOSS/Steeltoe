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

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Util
{
    public class SimpleRouteMatcher : IRouteMatcher
    {
        public SimpleRouteMatcher(IPathMatcher pathMatcher)
        {
            if (pathMatcher == null)
            {
                throw new ArgumentNullException(nameof(pathMatcher));
            }

            PathMatcher = pathMatcher;
        }

        public IPathMatcher PathMatcher { get; }

        public IRoute ParseRoute(string route)
        {
            return new DefaultRoute(route);
        }

        public bool IsPattern(string route)
        {
            return PathMatcher.IsPattern(route);
        }

        public string Combine(string pattern1, string pattern2)
        {
            return PathMatcher.Combine(pattern1, pattern2);
        }

        public bool Match(string pattern, IRoute route)
        {
            return PathMatcher.Match(pattern, route.Value);
        }

        public IDictionary<string, string> MatchAndExtract(string pattern, IRoute route)
        {
            if (!Match(pattern, route))
            {
                return null;
            }

            return PathMatcher.ExtractUriTemplateVariables(pattern, route.Value);
        }

        public IComparer<string> GetPatternComparer(IRoute route)
        {
            return PathMatcher.GetPatternComparer(route.Value);
        }

        private class DefaultRoute : IRoute
        {
            public DefaultRoute(string path)
            {
                Value = path;
            }

            public string Value { get; }
        }
    }
}
