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

        private static readonly Regex VARIABLE_PATTERN = new Regex("\\{[^/]+?\\}", RegexOptions.Compiled);

        private static readonly char[] WILDCARD_CHARS = { '*', '?', '{' };

        private readonly ConcurrentDictionary<string, string[]> _tokenizedPatternCache = new ConcurrentDictionary<string, string[]>();

        private readonly ConcurrentDictionary<string, AntPathStringMatcher> _stringMatcherCache = new ConcurrentDictionary<string, AntPathStringMatcher>();

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
            bool uriVar = false;
            for (int i = 0; i < path.Length; i++)
            {
                char c = path[i];
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
            string[] patternParts = TokenizePath(pattern);
            string[] pathParts = TokenizePath(path);

            StringBuilder builder = new StringBuilder();
            bool pathStarted = false;

            for (int segment = 0; segment < patternParts.Length; segment++)
            {
                string patternPart = patternParts[segment];
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
            Dictionary<string, string> variables = new Dictionary<string, string>();
            bool result = DoMatch(pattern, path, true, variables);
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

            bool pattern1ContainsUriVar = pattern1.IndexOf('{') != -1;
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

            int starDotPos1 = pattern1.IndexOf("*.");
            if (pattern1ContainsUriVar || starDotPos1 == -1 || _pathSeparator.Equals("."))
            {
                // simply concatenate the two patterns
                return Concat(pattern1, pattern2);
            }

            string ext1 = pattern1.Substring(starDotPos1 + 1);
            int dotPos2 = pattern2.IndexOf('.');
            string file2 = dotPos2 == -1 ? pattern2 : pattern2.Substring(0, dotPos2);
            string ext2 = dotPos2 == -1 ? string.Empty : pattern2.Substring(dotPos2);
            bool ext1All = ext1.Equals(".*") || ext1 == string.Empty;
            bool ext2All = ext2.Equals(".*") || ext2 == string.Empty;
            if (!ext1All && !ext2All)
            {
                throw new InvalidOperationException("Cannot combine patterns: " + pattern1 + " vs " + pattern2);
            }

            string ext = ext1All ? ext2 : ext1;
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

            string[] pattDirs = TokenizePattern(pattern);
            if (fullMatch && CaseSensitive && !IsPotentialMatch(path, pattDirs))
            {
                return false;
            }

            string[] pathDirs = TokenizePath(path);

            int pattIdxStart = 0;
            int pattIdxEnd = pattDirs.Length - 1;
            int pathIdxStart = 0;
            int pathIdxEnd = pathDirs.Length - 1;

            // Match all elements up to the first **
            while (pattIdxStart <= pattIdxEnd && pathIdxStart <= pathIdxEnd)
            {
                string pattDir = pattDirs[pattIdxStart];
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

                for (int i = pattIdxStart; i <= pattIdxEnd; i++)
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
                string pattDir = pattDirs[pattIdxEnd];
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
                for (int i = pattIdxStart; i <= pattIdxEnd; i++)
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
                int patIdxTmp = -1;
                for (int i = pattIdxStart + 1; i <= pattIdxEnd; i++)
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
                int patLength = patIdxTmp - pattIdxStart - 1;
                int strLength = pathIdxEnd - pathIdxStart + 1;
                int foundIdx = -1;

                for (int i = 0; i <= strLength - patLength; i++)
                {
                    bool failedMatch = false;
                    for (int j = 0; j < patLength; j++)
                    {
                        string subPat = pattDirs[pattIdxStart + j + 1];
                        string subStr = pathDirs[pathIdxStart + i + j];
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

            for (int i = pattIdxStart; i <= pattIdxEnd; i++)
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
            bool? cachePatterns = CachePatterns;
            if (cachePatterns == null || cachePatterns.Value)
            {
                _tokenizedPatternCache.TryGetValue(pattern, out tokenized);
            }

            if (tokenized == null)
            {
                tokenized = TokenizePath(pattern);
                if (cachePatterns == null && _tokenizedPatternCache.Count >= CACHE_TURNOFF_THRESHOLD)
                {
                    // Try to adapt to the runtime situation that we're encountering:
                    // There are obviously too many different patterns coming in here...
                    // So let's turn off the cache since the patterns are unlikely to be reoccurring.
                    DeactivatePatternCache();
                    return tokenized;
                }

                if (cachePatterns == null || cachePatterns.Value)
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
                for (int i = 0; i < split.Length; i++)
                {
                    split[i] = split[i].Trim();
                }
            }

            return split;
        }

        protected virtual AntPathStringMatcher GetStringMatcher(string pattern)
        {
            AntPathStringMatcher matcher = null;
            bool? cachePatterns = CachePatterns;
            if (cachePatterns == null || cachePatterns.Value)
            {
                _stringMatcherCache.TryGetValue(pattern, out matcher);
            }

            if (matcher == null)
            {
                matcher = new AntPathStringMatcher(pattern, CaseSensitive);
                if (cachePatterns == null && _stringMatcherCache.Count >= CACHE_TURNOFF_THRESHOLD)
                {
                    // Try to adapt to the runtime situation that we're encountering:
                    // There are obviously too many different patterns coming in here...
                    // So let's turn off the cache since the patterns are unlikely to be reoccurring.
                    DeactivatePatternCache();
                    return matcher;
                }

                if (cachePatterns == null || cachePatterns.Value)
                {
                    _stringMatcherCache.TryAdd(pattern, matcher);
                }
            }

            return matcher;
        }

        private string Concat(string path1, string path2)
        {
            bool path1EndsWithSeparator = path1.EndsWith(_pathSeparator);
            bool path2StartsWithSeparator = path2.StartsWith(_pathSeparator);

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
                int pos = 0;
                foreach (string pattDir in pattDirs)
                {
                    int skipped = SkipSeparator(path, pos, _pathSeparator);
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
            int skipped = 0;
            for (int i = 0; i < prefix.Length; i++)
            {
                char c = prefix[i];
                if (IsWildcardChar(c))
                {
                    return skipped;
                }

                int currPos = pos + skipped;
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
            int skipped = 0;
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
            foreach (char candidate in WILDCARD_CHARS)
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
            private readonly string path;

            public AntPatternComparator(string path)
            {
                this.path = path;
            }

            public int Compare(string pattern1, string pattern2)
            {
                PatternInfo info1 = new PatternInfo(pattern1);
                PatternInfo info2 = new PatternInfo(pattern2);

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

                bool pattern1EqualsPath = pattern1.Equals(path);
                bool pattern2EqualsPath = pattern2.Equals(path);
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
                private readonly string pattern;
                private readonly bool catchAllPattern;
                private int? length;

                public PatternInfo(string pattern)
                {
                    this.pattern = pattern;
                    if (this.pattern != null)
                    {
                        InitCounters();
                        catchAllPattern = this.pattern.Equals("/**");
                        IsPrefixPattern = !catchAllPattern && this.pattern.EndsWith("/**");
                    }

                    if (UriVars == 0)
                    {
                        length = this.pattern != null ? this.pattern.Length : 0;
                    }
                }

                protected void InitCounters()
                {
                    int pos = 0;
                    if (pattern != null)
                    {
                        while (pos < pattern.Length)
                        {
                            if (pattern[pos] == '{')
                            {
                                UriVars++;
                                pos++;
                            }
                            else if (pattern[pos] == '*')
                            {
                                if (pos + 1 < pattern.Length && pattern[pos + 1] == '*')
                                {
                                    DoubleWildcards++;
                                    pos += 2;
                                }
                                else if (pos > 0 && !pattern.Substring(pos - 1).Equals(".*"))
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

                public bool IsLeastSpecific
                {
                    get { return pattern == null || catchAllPattern; }
                }

                public bool IsPrefixPattern { get; }

                public int TotalCount
                {
                    get { return UriVars + SingleWildcards + (2 * DoubleWildcards); }
                }

                public int Length
                {
                    get
                    {
                        if (length == null)
                        {
                            length = pattern != null ?
                                    VARIABLE_PATTERN.Replace(pattern, "#").Length : 0;
                        }

                        return length.Value;
                    }
                }
            }
        }

        protected class AntPathStringMatcher
        {
            private const string DEFAULT_VARIABLE_PATTERN = "(.*)";
            private static readonly Regex GLOB_PATTERN = new Regex("\\?|\\*|\\{((?:\\{[^/]+?\\}|[^/{}]|\\\\[{}])+?)\\}", RegexOptions.Compiled);
            private readonly List<string> _variableNames = new List<string>();
            private readonly Regex pattern;

            public AntPathStringMatcher(string pattern)
            : this(pattern, true)
            {
            }

            public AntPathStringMatcher(string pattern, bool caseSensitive)
            {
                StringBuilder patternBuilder = new StringBuilder();
                var matcher = GLOB_PATTERN.Match(pattern);
                int end = 0;
                while (matcher.Success)
                {
                    patternBuilder.Append(Quote(pattern, end, matcher.Index));
                    string match = matcher.Value;
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
                        int colonIdx = match.IndexOf(':');
                        if (colonIdx == -1)
                        {
                            patternBuilder.Append(DEFAULT_VARIABLE_PATTERN);
                            var group = matcher.Groups[1];
                            _variableNames.Add(group.Value);
                        }
                        else
                        {
                            string variablePattern = match.Substring(colonIdx + 1, (match.Length - 1) - (colonIdx + 1));
                            patternBuilder.Append('(');
                            patternBuilder.Append(variablePattern);
                            patternBuilder.Append(')');
                            string variableName = match.Substring(1, colonIdx - 1);
                            _variableNames.Add(variableName);
                        }
                    }

                    end = matcher.Index + matcher.Length;
                    matcher = matcher.NextMatch();
                }

                patternBuilder.Append(Quote(pattern, end, pattern.Length));
                this.pattern = caseSensitive ? new Regex(patternBuilder.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled) :
                        new Regex(patternBuilder.ToString(), RegexOptions.Compiled);
            }

            public bool MatchStrings(string str, IDictionary<string, string> uriTemplateVariables)
            {
                var matcher = pattern.Match(str);
                if (matcher.Success && matcher.Length == str.Length)
                {
                    if (uriTemplateVariables != null)
                    {
                        // SPR-8455
                        if (_variableNames.Count != (matcher.Groups.Count - 1))
                        {
                            throw new InvalidOperationException("The number of capturing groups in the pattern segment " +
                                    pattern + " does not match the number of URI template variables it defines, " +
                                    "which can occur if capturing groups are used in a URI template regex. " +
                                    "Use non-capturing groups instead.");
                        }

                        for (int i = 1; i <= matcher.Groups.Count - 1; i++)
                        {
                            string name = _variableNames[i - 1];
                            string value = matcher.Groups[i].Value;
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

                string s = str.Substring(start, end - start);
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
