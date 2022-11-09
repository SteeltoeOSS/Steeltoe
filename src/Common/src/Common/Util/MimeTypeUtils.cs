// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text;

namespace Steeltoe.Common.Util;

public static class MimeTypeUtils
{
    public const string AllValue = "*/*";
    public const string ApplicationJsonValue = "application/json";
    public const string ApplicationOctetStreamValue = "application/octet-stream";
    public const string ApplicationXmlValue = "application/xml";
    public const string ImageGifValue = "image/gif";
    public const string ImageJpegValue = "image/jpeg";
    public const string ImagePngValue = "image/png";
    public const string TextHtmlValue = "text/html";
    public const string TextPlainValue = "text/plain";
    public const string TextXmlValue = "text/xml";

    private static readonly ConcurrentDictionary<string, MimeType> CachedMimeTypes = new();
    private static readonly char[] BoundaryChars = "-_1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
    public static readonly IComparer<MimeType> SpecificityComparator = new MimeType.SpecificityComparator<MimeType>();
    public static readonly MimeType All = new("*", "*");
    public static readonly MimeType ApplicationJson = new("application", "json");
    public static readonly MimeType ApplicationOctetStream = new("application", "octet-stream");
    public static readonly MimeType ApplicationXml = new("application", "xml");
    public static readonly MimeType ImageGif = new("image", "gif");
    public static readonly MimeType ImageJpeg = new("image", "jpeg");
    public static readonly MimeType ImagePng = new("image", "png");
    public static readonly MimeType TextHtml = new("text", "html");
    public static readonly MimeType TextPlain = new("text", "plain");
    public static readonly MimeType TextXml = new("text", "xml");

    public static MimeType ParseMimeType(string mimeType)
    {
        return CachedMimeTypes.GetOrAdd(mimeType, ParseMimeTypeInternal(mimeType));
    }

    public static List<MimeType> ParseMimeTypes(string mimeTypes)
    {
        if (string.IsNullOrEmpty(mimeTypes))
        {
            return new List<MimeType>();
        }

        List<string> tokens = Tokenize(mimeTypes);
        var results = new List<MimeType>();

        foreach (string token in tokens)
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
        bool inQuotes = false;
        int startIndex = 0;
        int i = 0;

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

    public static string ToString(ICollection<MimeType> mimeTypes)
    {
        var builder = new StringBuilder();

        foreach (MimeType mimeType in mimeTypes)
        {
            mimeType.AppendTo(builder);
            builder.Append(", ");
        }

        string built = builder.ToString();

        if (built.EndsWith(", ", StringComparison.Ordinal))
        {
            built = built.Substring(0, built.Length - 2);
        }

        return built;
    }

    public static void SortBySpecificity(List<MimeType> mimeTypes)
    {
        ArgumentGuard.NotNull(mimeTypes);

        if (mimeTypes.Count > 1)
        {
            mimeTypes.Sort(SpecificityComparator);
        }
    }

    public static char[] GenerateMultipartBoundary()
    {
        int size = Random.Shared.Next(11) + 30;
        char[] boundary = new char[size];

        for (int i = 0; i < boundary.Length; i++)
        {
            boundary[i] = BoundaryChars[Random.Shared.Next(BoundaryChars.Length)];
        }

        return boundary;
    }

    public static string GenerateMultipartBoundaryString()
    {
        return new string(GenerateMultipartBoundary());
    }

    private static MimeType ParseMimeTypeInternal(string mimeType)
    {
        ArgumentGuard.NotNullOrEmpty(mimeType);

        int index = mimeType.IndexOf(';');
        string fullType = (index >= 0 ? mimeType.Substring(0, index) : mimeType).Trim();

        if (string.IsNullOrEmpty(fullType))
        {
            throw new ArgumentException("mimeType must contain a value before the ';' separator.", nameof(mimeType));
        }

        if (fullType == MimeType.WildcardType)
        {
            fullType = "*/*";
        }

        int subIndex = fullType.IndexOf('/');

        if (subIndex == -1)
        {
            throw new ArgumentException($"{mimeType} does not contain '/'", nameof(mimeType));
        }

        if (subIndex == fullType.Length - 1)
        {
            throw new ArgumentException($"{mimeType} does not contain subtype after '/'", nameof(mimeType));
        }

        string type = fullType.Substring(0, subIndex);
        string subtype = fullType.Substring(subIndex + 1, fullType.Length - type.Length - 1);

        if (type == MimeType.WildcardType && subtype != MimeType.WildcardType)
        {
            throw new ArgumentException($"{mimeType} wildcard type is legal only in '*/*' (all mime types)", nameof(mimeType));
        }

        Dictionary<string, string> parameters = null;

        do
        {
            int nextIndex = index + 1;
            bool quoted = false;

            while (nextIndex < mimeType.Length)
            {
                char ch = mimeType[nextIndex];

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

            string parameter = mimeType.Substring(index + 1, nextIndex - index - 1).Trim();

            if (parameter.Length > 0)
            {
                parameters ??= new Dictionary<string, string>(4);

                int eqIndex = parameter.IndexOf('=');

                if (eqIndex >= 0)
                {
                    string attribute = parameter.Substring(0, eqIndex).Trim();
                    string value = parameter.Substring(eqIndex + 1, parameter.Length - eqIndex - 1).Trim();
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
            throw new ArgumentException($"The specified value '{mimeType}' is invalid.", ex);
        }
    }
}
