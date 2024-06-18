// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

public sealed class IgnoreLineEndingsComparer : IEqualityComparer<string>
{
    private static readonly string[] LineSeparators =
    {
        "\r\n",
        "\r",
        "\n"
    };

    public static readonly IgnoreLineEndingsComparer Instance = new();

    public bool Equals(string x, string y)
    {
        if (x == y)
        {
            return true;
        }

        if (x == null || y == null)
        {
            return false;
        }

        string[] xLines = x.Split(LineSeparators, StringSplitOptions.None);
        string[] yLines = y.Split(LineSeparators, StringSplitOptions.None);

        return xLines.SequenceEqual(yLines);
    }

    public int GetHashCode(string obj)
    {
        return HashCode.Combine(obj);
    }
}
