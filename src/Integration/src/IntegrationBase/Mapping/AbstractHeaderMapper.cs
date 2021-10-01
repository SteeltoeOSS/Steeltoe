// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Mapping
{
    public abstract class AbstractHeaderMapper<T> : IRequestReplyHeaderMapper<T>
    {
        public const string STANDARD_REQUEST_HEADER_NAME_PATTERN = "STANDARD_REQUEST_HEADERS";
        public const string STANDARD_REPLY_HEADER_NAME_PATTERN = "STANDARD_REPLY_HEADERS";
        public const string NON_STANDARD_HEADER_NAME_PATTERN = "NON_STANDARD_HEADERS";

        private readonly List<string> _transient_header_names = new () { MessageHeaders.ID, MessageHeaders.TIMESTAMP };

        private readonly ILogger _logger;

        public string StandardHeaderPrefix { get; set; }

        public List<string> RequestHeaderNames { get; set; }

        public List<string> ReplyHeaderNames { get; set; }

        public IHeaderMatcher RequestHeaderMatcher { get; set; }

        public IHeaderMatcher ReplyHeaderMatcher { get; set; }

        protected AbstractHeaderMapper(string standardHeaderPrefix, List<string> requestHeaderNames, List<string> replyHeaderNames, ILogger logger)
        {
            StandardHeaderPrefix = standardHeaderPrefix;
            RequestHeaderNames = requestHeaderNames;
            ReplyHeaderNames = replyHeaderNames;
            _logger = logger;

#pragma warning disable S1699 // Constructors should only call non-overridable methods
            RequestHeaderMatcher = CreateDefaultHeaderMatcher(StandardHeaderPrefix, RequestHeaderNames);
            ReplyHeaderMatcher = CreateDefaultHeaderMatcher(StandardHeaderPrefix, ReplyHeaderNames);
#pragma warning restore S1699 // Constructors should only call non-overridable methods
        }

        public void SetRequestHeaderNames(params string[] requestHeaderNames)
        {
            if (requestHeaderNames == null)
            {
                throw new ArgumentNullException(nameof(requestHeaderNames));
            }

            RequestHeaderMatcher = CreateHeaderMatcher(requestHeaderNames);
        }

        public void SetReplyHeaderNames(params string[] replyHeaderNames)
        {
            if (replyHeaderNames == null)
            {
                throw new ArgumentNullException(nameof(replyHeaderNames));
            }

            ReplyHeaderMatcher = CreateHeaderMatcher(replyHeaderNames);
        }

        public void FromHeadersToRequest(IMessageHeaders headers, T target)
        {
            FromHeaders(headers, target, RequestHeaderMatcher);
        }

        public void FromHeadersToReply(IMessageHeaders headers, T target)
        {
            FromHeaders(headers, target, ReplyHeaderMatcher);
        }

        public IDictionary<string, object> ToHeadersFromRequest(T source)
        {
            return ToHeaders(source, RequestHeaderMatcher);
        }

        public IDictionary<string, object> ToHeadersFromReply(T source)
        {
            return ToHeaders(source, ReplyHeaderMatcher);
        }

        protected virtual IHeaderMatcher CreateDefaultHeaderMatcher(string standardHeaderPrefix, List<string> headerNames)
        {
            return new ContentBasedHeaderMatcher(true, new List<string>(headerNames));
        }

        protected virtual IHeaderMatcher CreateHeaderMatcher(string[] patterns)
        {
            var matchers = new List<IHeaderMatcher>();
            foreach (var pattern in patterns)
            {
                if (STANDARD_REQUEST_HEADER_NAME_PATTERN.Equals(pattern))
                {
                    matchers.Add(new ContentBasedHeaderMatcher(true, RequestHeaderNames));
                }
                else if (STANDARD_REPLY_HEADER_NAME_PATTERN.Equals(pattern))
                {
                    matchers.Add(new ContentBasedHeaderMatcher(true, ReplyHeaderNames));
                }
                else if (NON_STANDARD_HEADER_NAME_PATTERN.Equals(pattern))
                {
                    matchers.Add(new PrefixBasedMatcher(false, StandardHeaderPrefix));
                }
                else
                {
                    var thePattern = pattern;
                    var negate = false;
                    if (pattern.StartsWith("!"))
                    {
                        thePattern = pattern[1..];
                        negate = true;
                    }
                    else if (pattern.StartsWith("\\!"))
                    {
                        thePattern = pattern[1..];
                    }

                    if (negate)
                    {
                        // negative matchers get priority
                        matchers.Insert(0, new SinglePatternBasedHeaderMatcher(thePattern, negate));
                    }
                    else
                    {
                        matchers.Add(new SinglePatternBasedHeaderMatcher(thePattern, negate));
                    }
                }
            }

            return new CompositeHeaderMatcher(matchers);
        }

        protected virtual V GetHeaderIfAvailable<V>(IDictionary<string, object> headers, string name, Type type)
        {
            headers.TryGetValue(name, out var value);
            if (value == null)
            {
                return default;
            }

            if (!type.IsInstanceOfType(value))
            {
                return default;
            }
            else
            {
                return (V)value;
            }
        }

        protected virtual string CreateTargetPropertyName(string propertyName, bool fromMessageHeaders)
        {
            return propertyName;
        }

        protected virtual List<string> GetTransientHeaderNames()
        {
            return _transient_header_names;
        }

        protected abstract IDictionary<string, object> ExtractStandardHeaders(T source);

        protected abstract IDictionary<string, object> ExtractUserDefinedHeaders(T source);

        protected abstract void PopulateStandardHeaders(IDictionary<string, object> headers, T target);

        protected virtual void PopulateStandardHeaders(IDictionary<string, object> allHeaders, IDictionary<string, object> subset, T target)
        {
            PopulateStandardHeaders(subset, target);
        }

        protected abstract void PopulateUserDefinedHeader(string headerName, object headerValue, T target);

        private static bool IsMessageChannel(object headerValue) => headerValue is IMessageChannel;

        private void FromHeaders(IMessageHeaders headers, T target, IHeaderMatcher headerMatcher)
        {
            try
            {
                var subset = new Dictionary<string, object>();
                foreach (var entry in (IDictionary<string, object>)headers)
                {
                    var headerName = entry.Key;
                    if (ShouldMapHeader(headerName, headerMatcher))
                    {
                        subset[headerName] = entry.Value;
                    }
                }

                PopulateStandardHeaders(headers, subset, target);
                PopulateUserDefinedHeaders(subset, target);
            }
            catch (Exception ex)
            {
                 _logger?.LogError(ex, ex.Message);
            }
        }

        private void PopulateUserDefinedHeaders(IDictionary<string, object> headers, T target)
        {
            foreach (var entry in headers)
            {
                var headerName = entry.Key;
                var value = entry.Value;
                if (value != null && !IsMessageChannel(value))
                {
                    try
                    {
                        if (!headerName.StartsWith(StandardHeaderPrefix))
                        {
                            var key = CreateTargetPropertyName(headerName, true);
                            PopulateUserDefinedHeader(key, value, target);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, ex.Message);
                    }
                }
            }
        }

        private Dictionary<string, object> ToHeaders(T source, IHeaderMatcher headerMatcher)
        {
            var headers = new Dictionary<string, object>();
            var standardHeaders = ExtractStandardHeaders(source);
            CopyHeaders(standardHeaders, headers, headerMatcher);
            var userDefinedHeaders = ExtractUserDefinedHeaders(source);
            CopyHeaders(userDefinedHeaders, headers, headerMatcher);
            return headers;
        }

        private void CopyHeaders(IDictionary<string, object> source, IDictionary<string, object> target, IHeaderMatcher headerMatcher)
        {
            if (source != null)
            {
                foreach (var entry in source)
                {
                    try
                    {
                        var headerName = CreateTargetPropertyName(entry.Key, false);
                        if (ShouldMapHeader(headerName, headerMatcher))
                        {
                            target[headerName] = entry.Value;
                        }
                    }
                    catch (Exception)
                    {
                        // Log
                    }
                }
            }
        }

        private bool ShouldMapHeader(string headerName, IHeaderMatcher headerMatcher)
        {
            return !(string.IsNullOrEmpty(headerName) || GetTransientHeaderNames().Contains(headerName)) && headerMatcher.MatchHeader(headerName);
        }

        public interface IHeaderMatcher
        {
            bool MatchHeader(string headerName);

            bool IsNegated { get; }
        }

        protected class ContentBasedHeaderMatcher : IHeaderMatcher
        {
            private bool Match { get; }

            private List<string> Content { get; }

            public ContentBasedHeaderMatcher(bool match, List<string> content)
            {
                if (content == null)
                {
                    throw new ArgumentNullException(nameof(content));
                }

                Match = match;
                Content = content;
            }

            public bool MatchHeader(string headerName)
            {
                var result = Match == ContainsIgnoreCase(headerName);
                return result;
            }

            public bool IsNegated => false;

            private bool ContainsIgnoreCase(string name)
            {
                foreach (var headerName in Content)
                {
                    if (headerName.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        protected class PatternBasedHeaderMatcher : IHeaderMatcher
        {
            private List<string> Patterns { get; } = new List<string>();

            public PatternBasedHeaderMatcher(List<string> patterns)
            {
                if (patterns == null)
                {
                    throw new ArgumentNullException(nameof(patterns));
                }

                if (patterns.Count == 0)
                {
                    throw new ArgumentException("At least one pattern must be specified");
                }

                foreach (var pattern in patterns)
                {
                    Patterns.Add(pattern.ToLower());
                }
            }

            public bool MatchHeader(string headerName)
            {
                var header = headerName.ToLower();
                foreach (var pattern in Patterns)
                {
                    if (PatternMatchUtils.SimpleMatch(pattern, header))
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsNegated => false;
        }

        protected class SinglePatternBasedHeaderMatcher : IHeaderMatcher
        {
            private string Pattern { get; }

            private bool Negate { get; }

            public SinglePatternBasedHeaderMatcher(string pattern)
            : this(pattern, false)
            {
            }

            public SinglePatternBasedHeaderMatcher(string pattern, bool negate)
            {
                if (pattern == null)
                {
                    throw new ArgumentNullException(nameof(pattern));
                }

                Pattern = pattern.ToLower();
                Negate = negate;
            }

            public bool MatchHeader(string headerName)
            {
                var header = headerName.ToLower();
                if (PatternMatchUtils.SimpleMatch(Pattern, header))
                {
                    return true;
                }

                return false;
            }

            public bool IsNegated => Negate;
        }

        protected class PrefixBasedMatcher : IHeaderMatcher
        {
            private bool Match { get; }

            private string Prefix { get; }

            public PrefixBasedMatcher(bool match, string prefix)
            {
                Match = match;
                Prefix = prefix;
            }

            public bool MatchHeader(string headerName)
            {
                var result = Match == headerName.StartsWith(Prefix);
                return result;
            }

            public bool IsNegated { get; } = false;
        }

        protected class CompositeHeaderMatcher : IHeaderMatcher
        {
            private List<IHeaderMatcher> Matchers { get; }

            public CompositeHeaderMatcher(List<IHeaderMatcher> strategies)
            {
                Matchers = strategies;
            }

            public CompositeHeaderMatcher(params IHeaderMatcher[] strategies)
            : this(new List<IHeaderMatcher>(strategies))
            {
            }

            public bool MatchHeader(string headerName)
            {
                foreach (var strategy in Matchers)
                {
                    if (strategy.MatchHeader(headerName))
                    {
                        if (strategy.IsNegated)
                        {
                            break;
                        }

                        return true;
                    }
                }

                return false;
            }

            public bool IsNegated { get; } = false;
        }
    }
}
