// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public class SimpleRouteMatcher : IRouteMatcher
{
    public SimpleRouteMatcher(IPathMatcher pathMatcher)
    {
        PathMatcher = pathMatcher ?? throw new ArgumentNullException(nameof(pathMatcher));
    }

    public IPathMatcher PathMatcher { get; }

    public IRoute ParseRoute(string routeValue)
    {
        return new DefaultRoute(routeValue);
    }

    public bool IsPattern(string routeValue)
    {
        return PathMatcher.IsPattern(routeValue);
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

    private sealed class DefaultRoute : IRoute
    {
        public DefaultRoute(string path)
        {
            Value = path;
        }

        public string Value { get; }
    }
}
