// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Handler.Test;

public class DestinationPatternsMessageConditionTest
{
    [Fact]
    public void PrependSlash()
    {
        DestinationPatternsMessageCondition c = Condition("foo");
        Assert.Equal("/foo", c.Patterns.Single());
    }

    [Fact]
    public void PrependSlashWithCustomPathSeparator()
    {
        var c = new DestinationPatternsMessageCondition(new[]
        {
            "foo"
        }, new AntPathMatcher("."));

        Assert.Equal("foo", c.Patterns.Single());
    }

    [Fact]
    public void PrependNonEmptyPatternsOnly()
    {
        DestinationPatternsMessageCondition c = Condition(string.Empty);
        Assert.Equal(string.Empty, c.Patterns.Single());
    }

    [Fact]
    public void CombineEmptySets()
    {
        DestinationPatternsMessageCondition c1 = Condition();
        DestinationPatternsMessageCondition c2 = Condition();
        Assert.Equal(Condition(string.Empty), c1.Combine(c2));
    }

    [Fact]
    public void CombineOnePatternWithEmptySet()
    {
        DestinationPatternsMessageCondition c1 = Condition("/type1", "/type2");
        DestinationPatternsMessageCondition c2 = Condition();

        Assert.Equal(Condition("/type1", "/type2"), c1.Combine(c2));

        c1 = Condition();
        c2 = Condition("/method1", "/method2");

        Assert.Equal(Condition("/method1", "/method2"), c1.Combine(c2));
    }

    [Fact]
    public void CombineMultiplePatterns()
    {
        DestinationPatternsMessageCondition c1 = Condition("/t1", "/t2");
        DestinationPatternsMessageCondition c2 = Condition("/m1", "/m2");

        Assert.Equal(new DestinationPatternsMessageCondition("/t1/m1", "/t1/m2", "/t2/m1", "/t2/m2"), c1.Combine(c2));
    }

    [Fact]
    public void MatchDirectPath()
    {
        DestinationPatternsMessageCondition condition = Condition("/foo");
        DestinationPatternsMessageCondition match = condition.GetMatchingCondition(MessageTo("/foo"));

        Assert.NotNull(match);
    }

    [Fact]
    public void MatchPattern()
    {
        DestinationPatternsMessageCondition condition = Condition("/foo/*");
        DestinationPatternsMessageCondition match = condition.GetMatchingCondition(MessageTo("/foo/bar"));

        Assert.NotNull(match);
    }

    [Fact]
    public void MatchSortPatterns()
    {
        DestinationPatternsMessageCondition condition = Condition("/**", "/foo/bar", "/foo/*");
        DestinationPatternsMessageCondition match = condition.GetMatchingCondition(MessageTo("/foo/bar"));
        DestinationPatternsMessageCondition expected = Condition("/foo/bar", "/foo/*", "/**");

        Assert.Equal(expected, match);
    }

    [Fact]
    public void CompareEqualPatterns()
    {
        DestinationPatternsMessageCondition c1 = Condition("/foo*");
        DestinationPatternsMessageCondition c2 = Condition("/foo*");

        Assert.Equal(0, c1.CompareTo(c2, MessageTo("/foo")));
    }

    [Fact]
    public void ComparePatternSpecificity()
    {
        DestinationPatternsMessageCondition c1 = Condition("/fo*");
        DestinationPatternsMessageCondition c2 = Condition("/foo");

        Assert.Equal(1, c1.CompareTo(c2, MessageTo("/foo")));
    }

    [Fact]
    public void CompareNumberOfMatchingPatterns()
    {
        IMessage message = MessageTo("/foo");

        DestinationPatternsMessageCondition c1 = Condition("/foo", "bar");
        DestinationPatternsMessageCondition c2 = Condition("/foo", "f*");

        DestinationPatternsMessageCondition match1 = c1.GetMatchingCondition(message);
        DestinationPatternsMessageCondition match2 = c2.GetMatchingCondition(message);

        Assert.Equal(1, match1.CompareTo(match2, message));
    }

    private DestinationPatternsMessageCondition Condition(params string[] patterns)
    {
        return new DestinationPatternsMessageCondition(patterns);
    }

    private IMessage MessageTo(string destination)
    {
        return MessageBuilder.WithPayload(Array.Empty<byte>()).SetHeader(DestinationPatternsMessageCondition.LookupDestinationHeader, destination).Build();
    }
}
