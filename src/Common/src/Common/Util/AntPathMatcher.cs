// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Steeltoe.Common.Util
{
    public class AntPathMatcher : IPathMatcher
    {
        public const string DEFAULT_PATH_SEPARATOR = "/";

        private const int CACHE_TURNOFF_THRESHOLD = 65536;

        private static readonly Regex VARIABLE_PATTERN = new ("\\{[^/]+?\\}", RegexOptions.Compiled);

        private static readonly char[] WILDCARD_CHARS = { '*', '?', '{' };

        private readonly ConcurrentDictionary<string, string[]> _tokenizedPatternCache = new ();

        private readonly ConcurrentDictionary<string, AntPathStringMatcher> _stringMatcherCache = new ();

        private string _pathSeparator;

        private PathSeparatorPatternCache _pathSeparatorPatternCache;

        public AntPathMatcher()
        {
            _pathSeparator = DEFAULT_PATH_SEPARATOR;
            _pathSeparatorPatternCache = new PathSeparatorPatternCache(DEFAULT_PATH_SEPARATOR);
        }

        public AntPathMatcher(string pathSeparator)
        {
            if (string.IsNullOrEmpty(pathSeparator))
            {
                throw new ArgumentException(nameof(pathSeparator));
            }

            _pathSeparator = pathSeparator;
            _pathSeparatorPatternCache = new PathSeparatorPatternCache(pathSeparator);
        }

        public virtual string PathSeparator
        {
            get
            {
                return _pathSeparator;
            }

            set
            {
                _pathSeparator = value ?? DEFAULT_PATH_SEPARATOR;
                _pathSeparatorPatternCache = new PathSeparatorPatternCache(_pathSeparator);
            }
        }

        public virtual bool CaseSensitive { get; set; } = true;

        public virtual bool TrimTokens { get; set; } = false;

        public virtual bool? CachePatterns { get; set; }

        public virtual bool IsPattern(string path)
        {
            var uriVar = false;
            foreach (var c in path)
            {
                if (c == '*' || c == '?')
                {
                    return true;
                }

                if (c == '{')
                {
                    uriVar = true;
                    continue;
                }

                if (c == '}' && uriVar)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool Match(string pattern, string path)
        {
            return DoMatch(pattern, path, true, null);
        }

        public virtual bool MatchStart(string pattern, string path)
        {
            return DoMatch(pattern, path, false, null);
        }

        public virtual string ExtractPathWithinPattern(string pattern, string path)
        {
            var patternParts = TokenizePath(pattern);
            var pathParts = TokenizePath(path);

            var builder = new StringBuilder();
            var pathStarted = false;

            for (var segment = 0; segment < patternParts.Length; segment++)
            {
                var patternPart = patternParts[segment];
                if (patternPart.IndexOf('*') > -1 || patternPart.IndexOf('?') > -1)
                {
                    for (; segment < pathParts.Length; segment++)
                    {
                        if (pathStarted || (segment == 0 && !pattern.StartsWith(_pathSeparator)))
                        {
                            builder.Append(_pathSeparator);
                        }

                        builder.Append(pathParts[segment]);
                        pathStarted = true;
                    }
                }
            }

            return builder.ToString();
        }

        public virtual IDictionary<string, string> ExtractUriTemplateVariables(string pattern, string path)
        {
            var variables = new Dictionary<string, string>();
            var result = DoMatch(pattern, path, true, variables);
            if (!result)
            {
                throw new InvalidOperationException("Pattern \"" + pattern + "\" is not a match for \"" + path + "\"");
            }

            return variables;
        }

        public virtual string Combine(string pattern1, string pattern2)
        {
            if (string.IsNullOrEmpty(pattern1) && string.IsNullOrEmpty(pattern2))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(pattern1))
            {
                return pattern2;
            }

            if (string.IsNullOrEmpty(pattern2))
            {
                return pattern1;
            }

            var pattern1ContainsUriVar = pattern1.IndexOf('{') != -1;
            if (!pattern1.Equals(pattern2) && !pattern1ContainsUriVar && Match(pattern1, pattern2))
            {
                // /* + /hotel -> /hotel ; "/*.*" + "/*.html" -> /*.html
                // However /user + /user -> /usr/user ; /{foo} + /bar -> /{foo}/bar
                return pattern2;
            }

            // /hotels/* + /booking -> /hotels/booking
            // /hotels/* + booking -> /hotels/booking
            if (pattern1.EndsWith(_pathSeparatorPatternCache.EndsOnWildCard))
            {
                return Concat(pattern1.Substring(0, pattern1.Length - 2), pattern2);
            }

            // /hotels/** + /booking -> /hotels/**/booking
            // /hotels/** + booking -> /hotels/**/booking
            if (pattern1.EndsWith(_pathSeparatorPatternCache.EndsOnDoubleWildCard))
            {
                return Concat(pattern1, pattern2);
            }

            var starDotPos1 = pattern1.IndexOf("*.");
            if (pattern1ContainsUriVar || starDotPos1 == -1 || _pathSeparator.Equals("."))
            {
                // simply concatenate the two patterns
                return Concat(pattern1, pattern2);
            }

            var ext1 = pattern1.Substring(starDotPos1 + 1);
            var dotPos2 = pattern2.IndexOf('.');
            var file2 = dotPos2 == -1 ? pattern2 : pattern2.Substring(0, dotPos2);
            var ext2 = dotPos2 == -1 ? string.Empty : pattern2.Substring(dotPos2);
            var ext1All = ext1.Equals(".*") || ext1 == string.Empty;
            var ext2All = ext2.Equals(".*") || ext2 == string.Empty;
            if (!ext1All && !ext2All)
            {
                throw new InvalidOperationException("Cannot combine patterns: " + pattern1 + " vs " + pattern2);
            }

            var ext = ext1All ? ext2 : ext1;
            return file2 + ext;
        }

        public virtual IComparer<string> GetPatternComparer(string path)
        {
            return new AntPatternComparator(path);
        }

        protected virtual bool DoMatch(string pattern, string path, bool fullMatch, IDictionary<string, string> uriTemplateVariables)
        {
            if (path.StartsWith(_pathSeparator) != pattern.StartsWith(_pathSeparator))
            {
                return false;
            }

            var pattDirs = TokenizePattern(pattern);
            if (fullMatch && CaseSensitive && !IsPotentialMatch(path, pattDirs))
            {
                return false;
            }

            var pathDirs = TokenizePath(path);

            var pattIdxStart = 0;
            var pattIdxEnd = pattDirs.Length - 1;
            var pathIdxStart = 0;
            var pathIdxEnd = pathDirs.Length - 1;

            // Match all elements up to the first **
            while (pattIdxStart <= pattIdxEnd && pathIdxStart <= pathIdxEnd)
            {
                var pattDir = pattDirs[pattIdxStart];
                if ("**".Equals(pattDir))
                {
                    break;
                }

                if (!MatchStrings(pattDir, pathDirs[pathIdxStart], uriTemplateVariables))
                {
                    return false;
                }

                pattIdxStart++;
                pathIdxStart++;
            }

            if (pathIdxStart > pathIdxEnd)
            {
                // Path is exhausted, only match if rest of pattern is * or **'s
                if (pattIdxStart > pattIdxEnd)
                {
                    return pattern.EndsWith(_pathSeparator) == path.EndsWith(_pathSeparator);
                }

                if (!fullMatch)
                {
                    return true;
                }

                if (pattIdxStart == pattIdxEnd && pattDirs[pattIdxStart].Equals("*") && path.EndsWith(_pathSeparator))
                {
                    return true;
                }

                for (var i = pattIdxStart; i <= pattIdxEnd; i++)
                {
                    if (!pattDirs[i].Equals("**"))
                    {
                        return false;
                    }
                }

                return true;
            }
            else if (pattIdxStart > pattIdxEnd)
            {
                // String not exhausted, but pattern is. Failure.
                return false;
            }
            else if (!fullMatch && "**".Equals(pattDirs[pattIdxStart]))
            {
                // Path start definitely matches due to "**" part in pattern.
                return true;
            }

            // up to last '**'
            while (pattIdxStart <= pattIdxEnd && pathIdxStart <= pathIdxEnd)
            {
                var pattDir = pattDirs[pattIdxEnd];
                if (pattDir.Equals("**"))
                {
                    break;
                }

                if (!MatchStrings(pattDir, pathDirs[pathIdxEnd], uriTemplateVariables))
                {
                    return false;
                }

                pattIdxEnd--;
                pathIdxEnd--;
            }

            if (pathIdxStart > pathIdxEnd)
            {
                // String is exhausted
                for (var i = pattIdxStart; i <= pattIdxEnd; i++)
                {
                    if (!pattDirs[i].Equals("**"))
                    {
                        return false;
                    }
                }

                return true;
            }

            while (pattIdxStart != pattIdxEnd && pathIdxStart <= pathIdxEnd)
            {
                var patIdxTmp = -1;
                for (var i = pattIdxStart + 1; i <= pattIdxEnd; i++)
                {
                    if (pattDirs[i].Equals("**"))
                    {
                        patIdxTmp = i;
                        break;
                    }
                }

                if (patIdxTmp == pattIdxStart + 1)
                {
                    // '**/**' situation, so skip one
                    pattIdxStart++;
                    continue;
                }

                // Find the pattern between padIdxStart & padIdxTmp in str between
                // strIdxStart & strIdxEnd
                var patLength = patIdxTmp - pattIdxStart - 1;
                var strLength = pathIdxEnd - pathIdxStart + 1;
                var foundIdx = -1;

                for (var i = 0; i <= strLength - patLength; i++)
                {
                    var failedMatch = false;
                    for (var j = 0; j < patLength; j++)
                    {
                        var subPat = pattDirs[pattIdxStart + j + 1];
                        var subStr = pathDirs[pathIdxStart + i + j];
                        if (!MatchStrings(subPat, subStr, uriTemplateVariables))
                        {
                            failedMatch = true;
                            break;
                        }
                    }

                    if (failedMatch)
                    {
                        continue;
                    }

                    foundIdx = pathIdxStart + i;
                    break;
                }

                if (foundIdx == -1)
                {
                    return false;
                }

                pattIdxStart = patIdxTmp;
                pathIdxStart = foundIdx + patLength;
            }

            for (var i = pattIdxStart; i <= pattIdxEnd; i++)
            {
                if (!pattDirs[i].Equals("**"))
                {
                    return false;
                }
            }

            return true;
        }

        protected virtual string[] TokenizePattern(string pattern)
        {
            string[] tokenized = null;
            if (CachePatterns == null || CachePatterns.Value)
            {
                _tokenizedPatternCache.TryGetValue(pattern, out tokenized);
            }

            if (tokenized == null)
            {
                tokenized = TokenizePath(pattern);
                if (CachePatterns == null && _tokenizedPatternCache.Count >= CACHE_TURNOFF_THRESHOLD)
                {
                    // Try to adapt to the runtime situation that we're encountering:
                    // There are obviously too many different patterns coming in here...
                    // So let's turn off the cache since the patterns are unlikely to be reoccurring.
                    DeactivatePatternCache();
                    return tokenized;
                }

                if (CachePatterns == null || CachePatterns.Value)
                {
                    _tokenizedPatternCache[pattern] = tokenized;
                }
            }

            return tokenized;
        }

        protected virtual string[] TokenizePath(string path)
        {
            if (path == null)
            {
                return Array.Empty<string>();
            }

            var split = path.Split(new string[] { _pathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            if (TrimTokens)
            {
                for (var i = 0; i < split.Length; i++)
                {
                    split[i] = split[i].Trim();
                }
            }

            return split;
        }

        protected virtual AntPathStringMatcher GetStringMatcher(string pattern)
        {
            AntPathStringMatcher matcher = null;
            if (CachePatterns == null || CachePatterns.Value)
            {
                _stringMatcherCache.TryGetValue(pattern, out matcher);
            }

            if (matcher == null)
            {
                matcher = new AntPathStringMatcher(pattern, CaseSensitive);
                if (CachePatterns == null && _stringMatcherCache.Count >= CACHE_TURNOFF_THRESHOLD)
                {
                    // Try to adapt to the runtime situation that we're encountering:
                    // There are obviously too many different patterns coming in here...
                    // So let's turn off the cache since the patterns are unlikely to be reoccurring.
                    DeactivatePatternCache();
                    return matcher;
                }

                if (CachePatterns == null || CachePatterns.Value)
                {
                    _stringMatcherCache.TryAdd(pattern, matcher);
                }
            }

            return matcher;
        }

        private string Concat(string path1, string path2)
        {
            var path1EndsWithSeparator = path1.EndsWith(_pathSeparator);
            var path2StartsWithSeparator = path2.StartsWith(_pathSeparator);

            if (path1EndsWithSeparator && path2StartsWithSeparator)
            {
                return path1 + path2.Substring(1);
            }
            else if (path1EndsWithSeparator || path2StartsWithSeparator)
            {
                return path1 + path2;
            }
            else
            {
                return path1 + _pathSeparator + path2;
            }
        }

        private bool MatchStrings(string pattern, string str, IDictionary<string, string> uriTemplateVariables)
        {
            return GetStringMatcher(pattern).MatchStrings(str, uriTemplateVariables);
        }

        private bool IsPotentialMatch(string path, string[] pattDirs)
        {
            if (!TrimTokens)
            {
                var pos = 0;
                foreach (var pattDir in pattDirs)
                {
                    var skipped = SkipSeparator(path, pos, _pathSeparator);
                    pos += skipped;
                    skipped = SkipSegment(path, pos, pattDir);
                    if (skipped < pattDir.Length)
                    {
                        return skipped > 0 || (pattDir.Length > 0 && IsWildcardChar(pattDir[0]));
                    }

                    pos += skipped;
                }
            }

            return true;
        }

        private int SkipSegment(string path, int pos, string prefix)
        {
            var skipped = 0;
            foreach (var c in prefix)
            {
                if (IsWildcardChar(c))
                {
                    return skipped;
                }

                var currPos = pos + skipped;
                if (currPos >= path.Length)
                {
                    return 0;
                }

                if (c == path[currPos])
                {
                    skipped++;
                }
            }

            return skipped;
        }

        private int SkipSeparator(string path, int pos, string separator)
        {
            var skipped = 0;
            path = path.Substring(pos);
            while (path.StartsWith(separator))
            {
                skipped += separator.Length;
                path = path.Substring(separator.Length);
            }

            return skipped;
        }

        private bool IsWildcardChar(char c)
        {
            foreach (var candidate in WILDCARD_CHARS)
            {
                if (c == candidate)
                {
                    return true;
                }
            }

            return false;
        }

        private void DeactivatePatternCache()
        {
            CachePatterns = false;
            _tokenizedPatternCache.Clear();
            _stringMatcherCache.Clear();
        }

        protected class AntPatternComparator : IComparer<string>
        {
            private readonly string _path;

            public AntPatternComparator(string path)
            {
                _path = path;
            }

            public int Compare(string pattern1, string pattern2)
            {
                var info1 = new PatternInfo(pattern1);
                var info2 = new PatternInfo(pattern2);

                if (info1.IsLeastSpecific && info2.IsLeastSpecific)
                {
                    return 0;
                }
                else if (info1.IsLeastSpecific)
                {
                    return 1;
                }
                else if (info2.IsLeastSpecific)
                {
                    return -1;
                }

                var pattern1EqualsPath = pattern1.Equals(_path);
                var pattern2EqualsPath = pattern2.Equals(_path);
                if (pattern1EqualsPath && pattern2EqualsPath)
                {
                    return 0;
                }
                else if (pattern1EqualsPath)
                {
                    return -1;
                }
                else if (pattern2EqualsPath)
                {
                    return 1;
                }

                if (info1.IsPrefixPattern && info2.DoubleWildcards == 0)
                {
                    return 1;
                }
                else if (info2.IsPrefixPattern && info1.DoubleWildcards == 0)
                {
                    return -1;
                }

                if (info1.TotalCount != info2.TotalCount)
                {
                    return info1.TotalCount - info2.TotalCount;
                }

                if (info1.Length != info2.Length)
                {
                    return info2.Length - info1.Length;
                }

                if (info1.SingleWildcards < info2.SingleWildcards)
                {
                    return -1;
                }
                else if (info2.SingleWildcards < info1.SingleWildcards)
                {
                    return 1;
                }

                if (info1.UriVars < info2.UriVars)
                {
                    return -1;
                }
                else if (info2.UriVars < info1.UriVars)
                {
                    return 1;
                }

                return 0;
            }

            private class PatternInfo
            {
                private readonly string _pattern;
                private bool _catchAllPattern;
                private int? _length;

                public PatternInfo(string pattern)
                {
                    _pattern = pattern;
                    if (_pattern != null)
                    {
                        InitCounters();
                        _catchAllPattern = _pattern.Equals("/**");
                        IsPrefixPattern = !_catchAllPattern && _pattern.EndsWith("/**");
                    }

                    if (UriVars == 0)
                    {
                        _length = _pattern != null ? _pattern.Length : 0;
                    }
                }

                protected void InitCounters()
                {
                    var pos = 0;
                    if (_pattern != null)
                    {
                        while (pos < _pattern.Length)
                        {
                            if (_pattern[pos] == '{')
                            {
                                UriVars++;
                                pos++;
                            }
                            else if (_pattern[pos] == '*')
                            {
                                if (pos + 1 < _pattern.Length && _pattern[pos + 1] == '*')
                                {
                                    DoubleWildcards++;
                                    pos += 2;
                                }
                                else if (pos > 0 && !_pattern.Substring(pos - 1).Equals(".*"))
                                {
                                    SingleWildcards++;
                                    pos++;
                                }
                                else
                                {
                                    pos++;
                                }
                            }
                            else
                            {
                                pos++;
                            }
                        }
                    }
                }

                public int UriVars { get; private set; }

                public int SingleWildcards { get; private set; }

                public int DoubleWildcards { get; private set; }

                public bool IsLeastSpecific => _pattern == null || _catchAllPattern;

                public bool IsPrefixPattern { get; }

                public int TotalCount => UriVars + SingleWildcards + (2 * DoubleWildcards);

                public int Length
                {
                    get
                    {
                        _length ??= _pattern != null ? VARIABLE_PATTERN.Replace(_pattern, "#").Length : 0;

                        return _length.Value;
                    }
                }
            }
        }

        protected class AntPathStringMatcher
        {
            private const string DEFAULT_VARIABLE_PATTERN = "(.*)";
            private static readonly Regex GLOB_PATTERN = new ("\\?|\\*|\\{((?:\\{[^/]+?\\}|[^/{}]|\\\\[{}])+?)\\}", RegexOptions.Compiled);
            private readonly List<string> _variableNames = new ();
            private Regex _pattern;

            public AntPathStringMatcher(string pattern)
            : this(pattern, true)
            {
            }

            public AntPathStringMatcher(string pattern, bool caseSensitive)
            {
                var patternBuilder = new StringBuilder();
                var matcher = GLOB_PATTERN.Match(pattern);
                var end = 0;
                while (matcher.Success)
                {
                    patternBuilder.Append(Quote(pattern, end, matcher.Index));
                    var match = matcher.Value;
                    if ("?".Equals(match))
                    {
                        patternBuilder.Append('.');
                    }
                    else if ("*".Equals(match))
                    {
                        patternBuilder.Append(".*");
                    }
                    else if (match.StartsWith("{") && match.EndsWith("}"))
                    {
                        var colonIdx = match.IndexOf(':');
                        if (colonIdx == -1)
                        {
                            patternBuilder.Append(DEFAULT_VARIABLE_PATTERN);
                            var group = matcher.Groups[1];
                            _variableNames.Add(group.Value);
                        }
                        else
                        {
                            var variablePattern = match.Substring(colonIdx + 1, (match.Length - 1) - (colonIdx + 1));
                            patternBuilder.Append('(');
                            patternBuilder.Append(variablePattern);
                            patternBuilder.Append(')');
                            var variableName = match.Substring(1, colonIdx - 1);
                            _variableNames.Add(variableName);
                        }
                    }

                    end = matcher.Index + matcher.Length;
                    matcher = matcher.NextMatch();
                }

                patternBuilder.Append(Quote(pattern, end, pattern.Length));
                _pattern = caseSensitive ? new Regex(patternBuilder.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled) :
                        new Regex(patternBuilder.ToString(), RegexOptions.Compiled);
            }

            public bool MatchStrings(string str, IDictionary<string, string> uriTemplateVariables)
            {
                var matcher = _pattern.Match(str);
                if (matcher.Success && matcher.Length == str.Length)
                {
                    if (uriTemplateVariables != null)
                    {
                        // SPR-8455
                        if (_variableNames.Count != (matcher.Groups.Count - 1))
                        {
                            throw new InvalidOperationException("The number of capturing groups in the pattern segment " +
                                    _pattern + " does not match the number of URI template variables it defines, " +
                                    "which can occur if capturing groups are used in a URI template regex. " +
                                    "Use non-capturing groups instead.");
                        }

                        for (var i = 1; i <= matcher.Groups.Count - 1; i++)
                        {
                            var name = _variableNames[i - 1];
                            var value = matcher.Groups[i].Value;
                            uriTemplateVariables[name] = value;
                        }
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }

            private string Quote(string str, int start, int end)
            {
                if (start == end)
                {
                    return string.Empty;
                }

                var s = str.Substring(start, end - start);
                return Regex.Escape(s);
            }
        }

        protected class PathSeparatorPatternCache
        {
            public PathSeparatorPatternCache(string pathSeparator)
            {
                EndsOnWildCard = pathSeparator + "*";
                EndsOnDoubleWildCard = pathSeparator + "**";
            }

            public string EndsOnWildCard { get; }

            public string EndsOnDoubleWildCard { get; }
        }
    }
}
