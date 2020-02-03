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

using Steeltoe.Common.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Steeltoe.Messaging.Handler
{
    public class DestinationPatternsMessageCondition : AbstractMessageCondition<DestinationPatternsMessageCondition>
    {
        public const string LOOKUP_DESTINATION_HEADER = "lookupDestination";
        private readonly IRouteMatcher _routeMatcher;

        public DestinationPatternsMessageCondition(params string[] patterns)
            : this(patterns, (IPathMatcher)null)
        {
        }

        public DestinationPatternsMessageCondition(string[] patterns, IPathMatcher matcher)
        : this(patterns, new SimpleRouteMatcher(matcher != null ? matcher : new AntPathMatcher()))
        {
        }

        public DestinationPatternsMessageCondition(string[] patterns, IRouteMatcher routeMatcher)
        : this(PrependLeadingSlash(patterns, routeMatcher), routeMatcher)
        {
        }

        private DestinationPatternsMessageCondition(ISet<string> patterns, IRouteMatcher routeMatcher)
        {
            Patterns = patterns;
            _routeMatcher = routeMatcher;
        }

        private static ISet<string> PrependLeadingSlash(string[] patterns, IRouteMatcher routeMatcher)
        {
            var slashSeparator = routeMatcher.Combine("a", "a").Equals("a/a");
            ISet<string> result = new HashSet<string>();
            foreach (var pat in patterns)
            {
                var pattern = pat;
                if (slashSeparator && !string.IsNullOrEmpty(pattern) && !pattern.StartsWith("/"))
                {
                    pattern = "/" + pattern;
                }

                result.Add(pattern);
            }

            return result;
        }

        public ISet<string> Patterns { get; }

        public override DestinationPatternsMessageCondition Combine(DestinationPatternsMessageCondition other)
        {
            ISet<string> result = new HashSet<string>();
            if (Patterns.Count > 0 && other.Patterns.Count > 0)
            {
                foreach (var pattern1 in Patterns)
                {
                    foreach (var pattern2 in other.Patterns)
                    {
                        result.Add(_routeMatcher.Combine(pattern1, pattern2));
                    }
                }
            }
            else if (Patterns.Count > 0)
            {
                result = new HashSet<string>(Patterns);
            }
            else if (other.Patterns.Count > 0)
            {
                result = new HashSet<string>(other.Patterns);
            }
            else
            {
                result.Add(string.Empty);
            }

            return new DestinationPatternsMessageCondition(result, _routeMatcher);
        }

        public override DestinationPatternsMessageCondition GetMatchingCondition(IMessage message)
        {
            message.Headers.TryGetValue(LOOKUP_DESTINATION_HEADER, out var destination);
            if (destination == null)
            {
                return null;
            }

            if (Patterns.Count == 0)
            {
                return this;
            }

            List<string> matches = null;
            foreach (var pattern in Patterns)
            {
                if (pattern.Equals(destination) || MatchPattern(pattern, destination))
                {
                    if (matches == null)
                    {
                        matches = new List<string>();
                    }

                    matches.Add(pattern);
                }
            }

            if (matches == null || matches.Count == 0)
            {
                return null;
            }

            matches.Sort(GetPatternComparer(destination));
            return new DestinationPatternsMessageCondition(new HashSet<string>(matches), _routeMatcher);
        }

        public override int CompareTo(DestinationPatternsMessageCondition other, IMessage message)
        {
            message.Headers.TryGetValue(LOOKUP_DESTINATION_HEADER, out var destination);
            if (destination == null)
            {
                return 0;
            }

            var patternComparator = GetPatternComparer(destination);
            var iterator = Patterns.GetEnumerator();
            var iteratorOther = other.Patterns.GetEnumerator();
            while (iterator.MoveNext() && iteratorOther.MoveNext())
            {
                var result = patternComparator.Compare(iterator.Current, iteratorOther.Current);
                if (result != 0)
                {
                    return result;
                }
            }

            if (iterator.MoveNext())
            {
                return -1;
            }
            else if (iteratorOther.MoveNext())
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        protected override IList GetContent()
        {
            return Patterns.ToList();
        }

        protected override string GetToStringInfix()
        {
            return " || ";
        }

        private bool MatchPattern(string pattern, object destination)
        {
            return destination is IRoute ?
                    _routeMatcher.Match(pattern, (IRoute)destination) :
                    ((SimpleRouteMatcher)_routeMatcher).PathMatcher.Match(pattern, (string)destination);
        }

        private IComparer<string> GetPatternComparer(object destination)
        {
            return destination is IRoute ?
                _routeMatcher.GetPatternComparer((IRoute)destination) :
                ((SimpleRouteMatcher)_routeMatcher).PathMatcher.GetPatternComparer((string)destination);
        }
    }
}
