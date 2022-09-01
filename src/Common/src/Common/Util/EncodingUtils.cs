// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Steeltoe.Common.Util;

public static class EncodingUtils
{
    public static readonly Encoding Utf8 = new UTF8Encoding(false);
    public static readonly Encoding Utf16 = new UnicodeEncoding(false, false);
    public static readonly Encoding Utf16BigEndian = new UnicodeEncoding(true, false);
    public static readonly Encoding Utf32 = new UTF32Encoding(false, false);
    public static readonly Encoding Utf32BigEndian = new UTF32Encoding(true, false);

    public static Encoding GetDefaultEncoding()
    {
        return GetEncoding(Encoding.Default.WebName);
    }

    public static Encoding GetEncoding(string name)
    {
        if (name == null)
        {
            return Utf8;
        }

        if (name.Equals("utf-7", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException("The UTF-7 encoding is insecure and should not be used. Consider using UTF-8 instead.");
        }

        if (name.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
        {
            return Utf8;
        }

        if (name.Equals("utf-16", StringComparison.OrdinalIgnoreCase))
        {
            return Utf16;
        }

        if (name.Equals("utf-16be", StringComparison.OrdinalIgnoreCase))
        {
            return Utf16BigEndian;
        }

        if (name.Equals("utf-32", StringComparison.OrdinalIgnoreCase))
        {
            return Utf32;
        }

        if (name.Equals("utf-32be", StringComparison.OrdinalIgnoreCase))
        {
            return Utf32BigEndian;
        }

        throw new ArgumentException($"Invalid encoding name '{name}'.", nameof(name));
    }

    public static string GetEncoding(Encoding encoding)
    {
        if (encoding == null || encoding.Equals(Utf8))
        {
            return "utf-8";
        }

        if (encoding.Equals(Utf16))
        {
            return "utf-16";
        }

        if (encoding.Equals(Utf16BigEndian))
        {
            return "utf-16be";
        }

        if (encoding.Equals(Utf32))
        {
            return "utf-32";
        }

        if (encoding.Equals(Utf32BigEndian))
        {
            return "utf-32be";
        }

        if (IsUtf7(encoding))
        {
            // https://docs.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib0001
            throw new NotSupportedException("The UTF-7 encoding is insecure and should not be used. Consider using UTF-8 instead.");
        }

        throw new ArgumentException($"Invalid encoding '{encoding.WebName}'.", nameof(encoding));
    }

    private static bool IsUtf7(Encoding encoding)
    {
        return encoding.CodePage == 65000;
    }
}
