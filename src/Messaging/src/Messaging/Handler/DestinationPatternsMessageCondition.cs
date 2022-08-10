// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Steeltoe.Common.Util;

namespace Steeltoe.Messaging.Handler;

public class DestinationPatternsMessageCondition : AbstractMessageCondition<DestinationPatternsMessageCondition>
{
    public const string LookupDestinationHeader = "lookupDestination";
    private readonly IRouteMatcher _routeMatcher;

    public ISet<string> Patterns { get; }

    public DestinationPatternsMessageCondition(params string[] patterns)
        : this(patterns, (IPathMatcher)null)
    {
    }

    public DestinationPatternsMessageCondition(string[] patterns, IPathMatcher matcher)
        : this(patterns, new SimpleRouteMatcher(matcher ?? new AntPathMatcher()))
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
        bool slashSeparator = routeMatcher.Combine("a", "a").Equals("a/a");
        ISet<string> result = new HashSet<string>();

        foreach (string pat in patterns)
        {
            string pattern = pat;

            if (slashSeparator && !string.IsNullOrEmpty(pattern) && !pattern.StartsWith("/"))
            {
                pattern = $"/{pattern}";
            }

            result.Add(pattern);
        }

        return result;
    }

    public override DestinationPatternsMessageCondition Combine(DestinationPatternsMessageCondition other)
    {
        ISet<string> result = new HashSet<string>();

        if (Patterns.Count > 0 && other.Patterns.Count > 0)
        {
            foreach (string pattern1 in Patterns)
            {
                foreach (string pattern2 in other.Patterns)
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
        message.Headers.TryGetValue(LookupDestinationHeader, out object destination);

        if (destination == null)
        {
            return null;
        }

        if (Patterns.Count == 0)
        {
            return this;
        }

        List<string> matches = null;

        foreach (string pattern in Patterns)
        {
            if (pattern.Equals(destination) || MatchPattern(pattern, destination))
            {
                matches ??= new List<string>();

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
        message.Headers.TryGetValue(LookupDestinationHeader, out object destination);

        if (destination == null)
        {
            return 0;
        }

        IComparer<string> patternComparator = GetPatternComparer(destination);
        using IEnumerator<string> iterator = Patterns.GetEnumerator();
        using IEnumerator<string> iteratorOther = other.Patterns.GetEnumerator();

        while (iterator.MoveNext() && iteratorOther.MoveNext())
        {
            int result = patternComparator.Compare(iterator.Current, iteratorOther.Current);

            if (result != 0)
            {
                return result;
            }
        }

        if (iterator.MoveNext())
        {
            return -1;
        }

        if (iteratorOther.MoveNext())
        {
            return 1;
        }

        return 0;
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
        return destination is IRoute route
            ? _routeMatcher.Match(pattern, route)
            : ((SimpleRouteMatcher)_routeMatcher).PathMatcher.Match(pattern, (string)destination);
    }

    private IComparer<string> GetPatternComparer(object destination)
    {
        return destination is IRoute route
            ? _routeMatcher.GetPatternComparer(route)
            : ((SimpleRouteMatcher)_routeMatcher).PathMatcher.GetPatternComparer((string)destination);
    }
}
