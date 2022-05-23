// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;
using System;
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
                    new DestinationPatternsMessageCondition(new[] { "foo" }, new AntPathMatcher("."));

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
            return MessageBuilder.WithPayload(Array.Empty<byte>()).SetHeader(
                    DestinationPatternsMessageCondition.LOOKUP_DESTINATION_HEADER, destination).Build();
        }
    }
}
