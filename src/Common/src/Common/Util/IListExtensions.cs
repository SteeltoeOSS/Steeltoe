// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

internal static class ListExtensions
{
    public static void AddRange<T>(this IList<T> source, IEnumerable<T> items)
    {
        ArgumentGuard.NotNull(source);
        ArgumentGuard.NotNull(items);

        if (source is List<T> list)
        {
            list.AddRange(items);
        }
        else
        {
            foreach (T item in items)
            {
                source.Add(item);
            }
        }
    }
}
