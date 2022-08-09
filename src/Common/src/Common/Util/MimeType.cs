// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Text;

namespace Steeltoe.Common.Util;
#pragma warning disable S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
public class MimeType : IComparable<MimeType>
#pragma warning restore S1210 // "Equals" and the comparison operators should be overridden when implementing "IComparable"
{
    private const string ParamCharset = "charset";
    public const string WildcardType = "*";

    private static readonly BitArray Token = new(128);

    private volatile string _toStringValue;

    public bool IsWildcardType => WildcardType.Equals(Type);

    public bool IsWildcardSubtype => WildcardType.Equals(Subtype) || Subtype.StartsWith("*+");

    public bool IsConcrete => !IsWildcardType && !IsWildcardSubtype;

    public string Type { get; }

    public string Subtype { get; }

    public Encoding Encoding
    {
        get
        {
            string charset = GetParameter(ParamCharset);
            return charset != null ? GetEncoding(Unquote(charset)) : null;
        }
    }

    public IDictionary<string, string> Parameters { get; }

    static MimeType()
    {
        // variable names refer to RFC 2616, section 2.2
        var ctl = new BitArray(128);

        for (int i = 0; i <= 31; i++)
        {
            ctl.Set(i, true);
        }

        ctl.Set(127, true);

        var separators = new BitArray(128);
        separators.Set('(', true);
        separators.Set(')', true);
        separators.Set('<', true);
        separators.Set('>', true);
        separators.Set('@', true);
        separators.Set(',', true);
        separators.Set(';', true);
        separators.Set(':', true);
        separators.Set('\\', true);
        separators.Set('\"', true);
        separators.Set('/', true);
        separators.Set('[', true);
        separators.Set(']', true);
        separators.Set('?', true);
        separators.Set('=', true);
        separators.Set('{', true);
        separators.Set('}', true);
        separators.Set(' ', true);
        separators.Set('\t', true);

        for (int i = 0; i < 128; i++)
        {
            Token.Set(i, true);
        }

        Token.And(ctl.Not());
        Token.And(separators.Not());
    }

    public MimeType(string type)
        : this(type, WildcardType)
    {
    }

    public MimeType(string type, string subtype)
        : this(type, subtype, new Dictionary<string, string>())
    {
    }

    public MimeType(string type, string subtype, Encoding charset)
        : this(type, subtype, new Dictionary<string, string>
        {
            { ParamCharset, charset.BodyName }
        })
    {
    }

    public MimeType(MimeType other, Encoding charset)
        : this(other.Type, other.Subtype, AddCharsetParameter(charset, other.Parameters))
    {
    }

    public MimeType(MimeType other, IDictionary<string, string> parameters = null)
        : this(other.Type, other.Subtype, parameters)
    {
    }

    public MimeType(string type, string subtype, IDictionary<string, string> parameters)
    {
        ArgumentGuard.NotNullOrEmpty(type);
        ArgumentGuard.NotNullOrEmpty(subtype);

        CheckToken(type);
        CheckToken(subtype);
        Type = type.ToLowerInvariant();
        Subtype = subtype.ToLowerInvariant();

        if (parameters.Count > 0)
        {
            var map = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> p in parameters)
            {
                CheckParameters(p.Key, p.Value);
                map.Add(p.Key, p.Value);
            }

            Parameters = map; // Read only
        }
        else
        {
            Parameters = new Dictionary<string, string>();
        }
    }

    public static MimeType ToMimeType(string value)
    {
        return MimeTypeUtils.ParseMimeType(value);
    }

    protected void CheckParameters(string attribute, string value)
    {
        ArgumentGuard.NotNullOrEmpty(attribute);
        ArgumentGuard.NotNullOrEmpty(value);

        CheckToken(attribute);

        if (ParamCharset.Equals(attribute))
        {
            value = Unquote(value);
            _ = Encoding.GetEncoding(value);
        }
        else if (!IsQuotedString(value))
        {
            CheckToken(value);
        }
    }

    protected string Unquote(string s)
    {
        return IsQuotedString(s) ? s.Substring(1, s.Length - 1 - 1) : s;
    }

    public string GetParameter(string name)
    {
        if (Parameters.TryGetValue(name, out string value))
        {
            return value;
        }

        return null;
    }

    public bool Includes(MimeType other)
    {
        if (other == null)
        {
            return false;
        }

        if (IsWildcardType)
        {
            // */* includes anything
            return true;
        }

        if (Type.Equals(other.Type))
        {
            if (Subtype.Equals(other.Subtype))
            {
                return true;
            }

            if (IsWildcardSubtype)
            {
                // Wildcard with suffix, e.g. application/*+xml
                int thisPlusIdx = Subtype.LastIndexOf('+');

                if (thisPlusIdx == -1)
                {
                    return true;
                }

                // application/*+xml includes application/soap+xml
                int otherPlusIdx = other.Subtype.LastIndexOf('+');

                if (otherPlusIdx != -1)
                {
                    string thisSubtypeNoSuffix = Subtype.Substring(0, thisPlusIdx);
                    string thisSubtypeSuffix = Subtype.Substring(thisPlusIdx + 1);
                    string otherSubtypeSuffix = other.Subtype.Substring(otherPlusIdx + 1);

                    if (thisSubtypeSuffix.Equals(otherSubtypeSuffix) && WildcardType.Equals(thisSubtypeNoSuffix))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool IsCompatibleWith(MimeType other)
    {
        if (other == null)
        {
            return false;
        }

        if (IsWildcardType || other.IsWildcardType)
        {
            return true;
        }

        if (Type.Equals(other.Type))
        {
            if (Subtype.Equals(other.Subtype))
            {
                return true;
            }

            // Wildcard with suffix? e.g. application/*+xml
            if (IsWildcardSubtype || other.IsWildcardSubtype)
            {
                int thisPlusIdx = Subtype.LastIndexOf('+');
                int otherPlusIdx = other.Subtype.LastIndexOf('+');

                if (thisPlusIdx == -1 && otherPlusIdx == -1)
                {
                    return true;
                }

                if (thisPlusIdx != -1 && otherPlusIdx != -1)
                {
                    string thisSubtypeNoSuffix = Subtype.Substring(0, thisPlusIdx);
                    string otherSubtypeNoSuffix = other.Subtype.Substring(0, otherPlusIdx);
                    string thisSubtypeSuffix = Subtype.Substring(thisPlusIdx + 1);
                    string otherSubtypeSuffix = other.Subtype.Substring(otherPlusIdx + 1);

                    if (thisSubtypeSuffix.Equals(otherSubtypeSuffix) && (WildcardType.Equals(thisSubtypeNoSuffix) || WildcardType.Equals(otherSubtypeNoSuffix)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public bool EqualsTypeAndSubtype(MimeType other)
    {
        if (other == null)
        {
            return false;
        }

        return Type.Equals(other.Type, StringComparison.InvariantCultureIgnoreCase) &&
            Subtype.Equals(other.Subtype, StringComparison.InvariantCultureIgnoreCase);
    }

    public bool IsPresentIn<T>(ICollection<T> mimeTypes)
        where T : MimeType
    {
        foreach (T mimeType in mimeTypes)
        {
            if (mimeType.EqualsTypeAndSubtype(this))
            {
                return true;
            }
        }

        return false;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not MimeType otherType)
        {
            return false;
        }

        return Type.Equals(otherType.Type, StringComparison.InvariantCultureIgnoreCase) &&
            Subtype.Equals(otherType.Subtype, StringComparison.InvariantCultureIgnoreCase) && ParametersAreEqual(otherType);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Subtype, Parameters);
    }

    public override string ToString()
    {
        string value = _toStringValue;

        if (value == null)
        {
            var builder = new StringBuilder();
            AppendTo(builder);
            value = builder.ToString();
            _toStringValue = value;
        }

        return value;
    }

    public int CompareTo(MimeType other)
    {
        int comp = Type.CompareTo(other.Type);

        if (comp != 0)
        {
            return comp;
        }

        comp = Subtype.CompareTo(other.Subtype);

        if (comp != 0)
        {
            return comp;
        }

        comp = Parameters.Count - other.Parameters.Count;

        if (comp != 0)
        {
            return comp;
        }

        var thisAttributes = new SortedSet<string>(Parameters.Keys, StringComparer.InvariantCultureIgnoreCase);
        var otherAttributes = new SortedSet<string>(other.Parameters.Keys, StringComparer.InvariantCultureIgnoreCase);

        using SortedSet<string>.Enumerator thisAttributesIterator = thisAttributes.GetEnumerator();
        using SortedSet<string>.Enumerator otherAttributesIterator = otherAttributes.GetEnumerator();
        StringComparer comparer = StringComparer.InvariantCultureIgnoreCase;

        while (thisAttributesIterator.MoveNext())
        {
            otherAttributesIterator.MoveNext();

            string thisAttribute = thisAttributesIterator.Current;
            string otherAttribute = otherAttributesIterator.Current;

            comp = comparer.Compare(thisAttribute, otherAttribute);

            if (comp != 0)
            {
                return comp;
            }

            if (ParamCharset.Equals(thisAttribute))
            {
                Encoding thisCharset = Encoding;
                Encoding otherCharset = other.Encoding;

                if (!Equals(thisCharset, otherCharset))
                {
                    if (thisCharset == null)
                    {
                        return -1;
                    }

                    if (otherCharset == null)
                    {
                        return 1;
                    }

                    comp = comparer.Compare(thisCharset.BodyName, otherCharset.BodyName);

                    if (comp != 0)
                    {
                        return comp;
                    }
                }
            }
            else
            {
                string thisValue = Parameters[thisAttribute];
                string otherValue = other.Parameters[otherAttribute] ?? string.Empty;

                comp = thisValue.CompareTo(otherValue);

                if (comp != 0)
                {
                    return comp;
                }
            }
        }

        return 0;
    }

    internal void AppendTo(StringBuilder builder)
    {
        builder.Append(Type);
        builder.Append('/');
        builder.Append(Subtype);
        AppendTo(Parameters, builder);
    }

    private static IDictionary<string, string> AddCharsetParameter(Encoding charset, IDictionary<string, string> parameters)
    {
        IDictionary<string, string> map = new Dictionary<string, string>(parameters)
        {
            [ParamCharset] = charset.BodyName
        };

        return map;
    }

    private bool IsQuotedString(string s)
    {
        if (s.Length < 2)
        {
            return false;
        }

        return (s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'"));
    }

    private bool ParametersAreEqual(MimeType other)
    {
        if (Parameters.Count != other.Parameters.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, string> entry in Parameters)
        {
            string key = entry.Key;

            if (!other.Parameters.ContainsKey(key))
            {
                return false;
            }

            if (ParamCharset.Equals(key))
            {
                if (!ObjectUtils.NullSafeEquals(Encoding, other.Encoding))
                {
                    return false;
                }
            }
            else if (!ObjectUtils.NullSafeEquals(entry.Value, other.Parameters[key]))
            {
                return false;
            }
        }

        return true;
    }

    private void CheckToken(string token)
    {
        foreach (char ch in token)
        {
            if (!Token.Get(ch))
            {
                throw new ArgumentException($"Invalid token character '{ch}' in token \"{token}\"");
            }
        }
    }

    private Encoding GetEncoding(string name)
    {
        if (name.Equals("utf-16", StringComparison.InvariantCultureIgnoreCase))
        {
            return EncodingUtils.Utf16;
        }

        if (name.Equals("utf-16be", StringComparison.InvariantCultureIgnoreCase))
        {
            return EncodingUtils.Utf16BigEndian;
        }

        if (name.Equals("utf-7", StringComparison.InvariantCultureIgnoreCase))
        {
            return EncodingUtils.Utf7;
        }

        if (name.Equals("utf-8", StringComparison.InvariantCultureIgnoreCase))
        {
            return EncodingUtils.Utf8;
        }

        if (name.Equals("utf-32", StringComparison.InvariantCultureIgnoreCase))
        {
            return EncodingUtils.Utf32;
        }

        if (name.Equals("utf-32BE", StringComparison.InvariantCultureIgnoreCase))
        {
            return EncodingUtils.Utf32BigEndian;
        }

        return Encoding.GetEncoding(name);
    }

    private void AppendTo(IDictionary<string, string> map, StringBuilder builder)
    {
        foreach (KeyValuePair<string, string> entry in map)
        {
            builder.Append(';');
            builder.Append(entry.Key);
            builder.Append('=');
            builder.Append(entry.Value);
        }
    }

    public class SpecificityComparator<T> : IComparer<T>
        where T : MimeType
    {
        public int Compare(T x, T y)
        {
            if (x.IsWildcardType && !y.IsWildcardType)
            {
                // */* < audio/*
                return 1;
            }

            if (y.IsWildcardType && !x.IsWildcardType)
            {
                // audio/* > */*
                return -1;
            }

            if (!x.Type.Equals(y.Type))
            {
                // audio/basic == text/html
                return 0;
            }

            // mediaType1.getType().Equals(mediaType2.getType())
            if (x.IsWildcardSubtype && !y.IsWildcardSubtype)
            {
                // audio/* < audio/basic
                return 1;
            }

            if (y.IsWildcardSubtype && !x.IsWildcardSubtype)
            {
                // audio/basic > audio/*
                return -1;
            }

            if (!x.Subtype.Equals(y.Subtype))
            {
                // audio/basic == audio/wave
                return 0;
            }

            // mediaType2.Subtype.Equals(mediaType2.Subtype)
            return CompareParameters(x, y);
        }

        protected int CompareParameters(T mimeType1, T mimeType2)
        {
            int paramsSize1 = mimeType1.Parameters.Count;
            int paramsSize2 = mimeType2.Parameters.Count;
            return paramsSize1.CompareTo(paramsSize2); // audio/basic;level=1 < audio/basic
        }
    }
}
