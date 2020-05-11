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
using Steeltoe.Messaging.Support;
using System.Linq;
using Xunit;

namespace Steeltoe.Messaging.Handler.Test
{
    public class DestinationPatternsMessageConditionTest
    {
        [Fact]
        public void PrependSlash()
        {
            var c = Condition("foo");
            Assert.Equal("/foo", c.Patterns.Single());
        }

        [Fact]
        public void PrependSlashWithCustomPathSeparator()
        {
            var c =
                    new DestinationPatternsMessageCondition(new string[] { "foo" }, new AntPathMatcher("."));

            Assert.Equal("foo", c.Patterns.Single());
        }

        [Fact]
        public void PrependNonEmptyPatternsOnly()
        {
            var c = Condition(string.Empty);
            Assert.Equal(string.Empty, c.Patterns.Single());
        }

        [Fact]
        public void CombineEmptySets()
        {
            var c1 = Condition();
            var c2 = Condition();
            Assert.Equal(Condition(string.Empty), c1.Combine(c2));
        }

        [Fact]
        public void CombineOnePatternWithEmptySet()
        {
            var c1 = Condition("/type1", "/type2");
            var c2 = Condition();

            Assert.Equal(Condition("/type1", "/type2"), c1.Combine(c2));

            c1 = Condition();
            c2 = Condition("/method1", "/method2");

            Assert.Equal(Condition("/method1", "/method2"), c1.Combine(c2));
        }

        [Fact]
        public void CombineMultiplePatterns()
        {
            var c1 = Condition("/t1", "/t2");
            var c2 = Condition("/m1", "/m2");

            Assert.Equal(new DestinationPatternsMessageCondition("/t1/m1", "/t1/m2", "/t2/m1", "/t2/m2"), c1.Combine(c2));
        }

        [Fact]
        public void MatchDirectPath()
        {
            var condition = Condition("/foo");
            var match = condition.GetMatchingCondition(MessageTo("/foo"));

            Assert.NotNull(match);
        }

        [Fact]
        public void MatchPattern()
        {
            var condition = Condition("/foo/*");
            var match = condition.GetMatchingCondition(MessageTo("/foo/bar"));

            Assert.NotNull(match);
        }

        [Fact]
        public void MatchSortPatterns()
        {
            var condition = Condition("/**", "/foo/bar", "/foo/*");
            var match = condition.GetMatchingCondition(MessageTo("/foo/bar"));
            var expected = Condition("/foo/bar", "/foo/*", "/**");

            Assert.Equal(expected, match);
        }

        [Fact]
        public void CompareEqualPatterns()
        {
            var c1 = Condition("/foo*");
            var c2 = Condition("/foo*");

            Assert.Equal(0, c1.CompareTo(c2, MessageTo("/foo")));
        }

        [Fact]
        public void ComparePatternSpecificity()
        {
            var c1 = Condition("/fo*");
            var c2 = Condition("/foo");

            Assert.Equal(1, c1.CompareTo(c2, MessageTo("/foo")));
        }

        [Fact]
        public void CompareNumberOfMatchingPatterns()
        {
            var message = MessageTo("/foo");

            var c1 = Condition("/foo", "bar");
            var c2 = Condition("/foo", "f*");

            var match1 = c1.GetMatchingCondition(message);
            var match2 = c2.GetMatchingCondition(message);

            Assert.Equal(1, match1.CompareTo(match2, message));
        }

        private DestinationPatternsMessageCondition Condition(params string[] patterns)
        {
            return new DestinationPatternsMessageCondition(patterns);
        }

        private IMessage MessageTo(string destination)
        {
            return MessageBuilder.WithPayload(System.Array.Empty<byte>()).SetHeader(
                    DestinationPatternsMessageCondition.LOOKUP_DESTINATION_HEADER, destination).Build();
        }
    }
}
