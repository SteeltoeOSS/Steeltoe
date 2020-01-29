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
        private readonly IPathMatcher pathMatcher;

        public SimpleRouteMatcher(IPathMatcher pathMatcher)
        {
            if (pathMatcher == null)
            {
                throw new ArgumentNullException(nameof(pathMatcher));
            }

            this.pathMatcher = pathMatcher;
        }

        public IPathMatcher PathMatcher
        {
            get { return pathMatcher; }
        }

        public IRoute ParseRoute(string route)
        {
            return new DefaultRoute(route);
        }

        public bool IsPattern(string route)
        {
            return pathMatcher.IsPattern(route);
        }

        public string Combine(string pattern1, string pattern2)
        {
            return pathMatcher.Combine(pattern1, pattern2);
        }

        public bool Match(string pattern, IRoute route)
        {
            return pathMatcher.Match(pattern, route.Value);
        }

        public IDictionary<string, string> MatchAndExtract(string pattern, IRoute route)
        {
            if (!Match(pattern, route))
            {
                return null;
            }

            return pathMatcher.ExtractUriTemplateVariables(pattern, route.Value);
        }

        public IComparer<string> GetPatternComparer(IRoute route)
        {
            return pathMatcher.GetPatternComparer(route.Value);
        }

        private class DefaultRoute : IRoute
        {
            private readonly string path;

            public DefaultRoute(string path)
            {
                this.path = path;
            }

            public string Value
            {
                get { return path; }
            }
        }
    }
}
