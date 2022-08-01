// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public interface IPathMatcher
{
    bool IsPattern(string path);

    bool Match(string pattern, string path);

    bool MatchStart(string pattern, string path);

    string ExtractPathWithinPattern(string pattern, string path);

    IDictionary<string, string> ExtractUriTemplateVariables(string pattern, string path);

    IComparer<string> GetPatternComparer(string path);

    string Combine(string pattern1, string pattern2);
}
