// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Common.Util;

public interface IRouteMatcher
{
    IRoute ParseRoute(string routeValue);

    bool IsPattern(string route);

    string Combine(string pattern1, string pattern2);

    bool Match(string pattern, IRoute route);

    IDictionary<string, string> MatchAndExtract(string pattern, IRoute route);

    IComparer<string> GetPatternComparer(IRoute route);
}