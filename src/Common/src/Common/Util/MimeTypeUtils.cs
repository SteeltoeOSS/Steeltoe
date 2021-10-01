// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Common.Util
{
    public static class MimeTypeUtils
    {
        public static readonly IComparer<MimeType> SPECIFICITY_COMPARATOR = new MimeType.SpecificityComparator<MimeType>();
        public static readonly MimeType ALL = new("*", "*");
        public static readonly string ALL_VALUE = "*/*";
        public static readonly MimeType APPLICATION_JSON = new("application", "json");
        public static readonly string APPLICATION_JSON_VALUE = "application/json";
        public static readonly MimeType APPLICATION_OCTET_STREAM = new("application", "octet-stream");
        public static readonly string APPLICATION_OCTET_STREAM_VALUE = "application/octet-stream";
        public static readonly MimeType APPLICATION_XML = new("application", "xml");
        public static readonly string APPLICATION_XML_VALUE = "application/xml";
        public static readonly MimeType IMAGE_GIF = new("image", "gif");
        public static readonly string IMAGE_GIF_VALUE = "image/gif";
        public static readonly MimeType IMAGE_JPEG = new("image", "jpeg");
        public static readonly string IMAGE_JPEG_VALUE = "image/jpeg";
        public static readonly MimeType IMAGE_PNG = new("image", "png");
        public static readonly string IMAGE_PNG_VALUE = "image/png";
        public static readonly MimeType TEXT_HTML = new("text", "html");
        public static readonly string TEXT_HTML_VALUE = "text/html";
        public static readonly MimeType TEXT_PLAIN = new("text", "plain");
        public static readonly string TEXT_PLAIN_VALUE = "text/plain";
        public static readonly MimeType TEXT_XML = new("text", "xml");
        public static readonly string TEXT_XML_VALUE = "text/xml";
        private static readonly ConcurrentDictionary<string, MimeType> _cachedMimeTypes = new ();
        private static readonly char[] BOUNDARY_CHARS =
             new char[]
            {
                '-', '_', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', 'a', 'b', 'c', 'd', 'e', 'f', 'g',
                'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'A',
                'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U',
                'V', 'W', 'X', 'Y', 'Z'
            };

        private static volatile Random _random;

        private static object _lock = new ();

        public static MimeType ParseMimeType(string mimeType)
        {
            return _cachedMimeTypes.GetOrAdd(mimeType, ParseMimeTypeInternal(mimeType));
        }

        public static List<MimeType> ParseMimeTypes(string mimeTypes)
        {
            if (string.IsNullOrEmpty(mimeTypes))
            {
                return new List<MimeType>();
            }

            var tokens = Tokenize(mimeTypes);
            var results = new List<MimeType>();
            foreach (var token in tokens)
            {
                results.Add(ParseMimeType(token));
            }

            return results;
        }

        public static List<string> Tokenize(string mimeTypes)
        {
            if (string.IsNullOrEmpty(mimeTypes))
            {
                return new List<string>();
            }

            var tokens = new List<string>();
            var inQuotes = false;
            var startIndex = 0;
            var i = 0;
            while (i < mimeTypes.Length)
            {
                switch (mimeTypes[i])
                {
                    case '"':
                        inQuotes = !inQuotes;
                        break;
                    case ',':
                        if (!inQuotes)
                        {
                            tokens.Add(mimeTypes.Substring(startIndex, i - startIndex));
                            startIndex = i + 1;
                        }

                        break;
                    case '\\':
                        i++;
                        break;
                }

                i++;
            }

            tokens.Add(mimeTypes.Substring(startIndex));
            return tokens;
        }

        public static string Tostring(ICollection<MimeType> mimeTypes)
        {
            var builder = new StringBuilder();
            foreach (var mimeType in mimeTypes)
            {
                mimeType.AppendTo(builder);
                builder.Append(", ");
            }

            var built = builder.ToString();
            if (built.EndsWith(", "))
            {
                built = built.Substring(0, built.Length - 2);
            }

            return built;
        }

        public static void SortBySpecificity(List<MimeType> mimeTypes)
        {
            if (mimeTypes == null)
            {
                throw new ArgumentNullException(nameof(mimeTypes));
            }

            if (mimeTypes.Count > 1)
            {
                mimeTypes.Sort(SPECIFICITY_COMPARATOR);
            }
        }

        public static char[] GenerateMultipartBoundary()
        {
            var randomToUse = InitRandom();
            var size = randomToUse.Next(11) + 30;
            var boundary = new char[size];
            for (var i = 0; i < boundary.Length; i++)
            {
                boundary[i] = BOUNDARY_CHARS[randomToUse.Next(BOUNDARY_CHARS.Length)];
            }

            return boundary;
        }

        public static string GenerateMultipartBoundaryString()
        {
            return new string(GenerateMultipartBoundary());
        }

        private static MimeType ParseMimeTypeInternal(string mimeType)
        {
            if (string.IsNullOrEmpty(mimeType))
            {
                throw new ArgumentException("'mimeType' must not be empty");
            }

            var index = mimeType.IndexOf(';');
            var fullType = (index >= 0 ? mimeType.Substring(0, index) : mimeType).Trim();
            if (string.IsNullOrEmpty(fullType))
            {
                throw new ArgumentException(mimeType, "'mimeType' must not be empty");
            }

            if (MimeType.WILDCARD_TYPE.Equals(fullType))
            {
                fullType = "*/*";
            }

            var subIndex = fullType.IndexOf('/');
            if (subIndex == -1)
            {
                throw new ArgumentException(mimeType + " does not contain '/'");
            }

            if (subIndex == fullType.Length - 1)
            {
                throw new ArgumentException(mimeType + " does not contain subtype after '/'");
            }

            var type = fullType.Substring(0, subIndex);
            var subtype = fullType.Substring(subIndex + 1, fullType.Length - type.Length - 1);
            if (MimeType.WILDCARD_TYPE.Equals(type) && !MimeType.WILDCARD_TYPE.Equals(subtype))
            {
                throw new ArgumentException(mimeType + " wildcard type is legal only in '*/*' (all mime types)");
            }

            Dictionary<string, string> parameters = null;
            do
            {
                var nextIndex = index + 1;
                var quoted = false;
                while (nextIndex < mimeType.Length)
                {
                    var ch = mimeType[nextIndex];
                    if (ch == ';')
                    {
                        if (!quoted)
                        {
                            break;
                        }
                    }
                    else if (ch == '"')
                    {
                        quoted = !quoted;
                    }

                    nextIndex++;
                }

                var parameter = mimeType.Substring(index + 1, nextIndex - index - 1).Trim();
                if (parameter.Length > 0)
                {
                    if (parameters == null)
                    {
                        parameters = new Dictionary<string, string>(4);
                    }

                    var eqIndex = parameter.IndexOf('=');
                    if (eqIndex >= 0)
                    {
                        var attribute = parameter.Substring(0, eqIndex).Trim();
                        var value = parameter.Substring(eqIndex + 1, parameter.Length - eqIndex - 1).Trim();
                        parameters[attribute] = value;
                    }
                }

                index = nextIndex;
            }
            while (index < mimeType.Length);

            try
            {
                return new MimeType(type, subtype, parameters);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(mimeType + " " + ex.Message);
            }
        }

        private static Random InitRandom()
        {
            var randomToUse = _random;
            if (randomToUse == null)
            {
                lock (_lock)
                {
                    randomToUse = _random;
                    if (randomToUse == null)
                    {
                        randomToUse = new Random();
                        _random = randomToUse;
                    }
                }
            }

            return randomToUse;
        }
    }
}
