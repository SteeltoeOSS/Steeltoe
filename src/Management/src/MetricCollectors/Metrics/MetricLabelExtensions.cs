// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.MetricCollectors.Metrics;

public static class MetricLabelExtensions
{
    internal static ReadOnlySpan<KeyValuePair<string, object>> AsReadonlySpan(this IDictionary<string, object> keyValuePairs)
    {
        return new ReadOnlySpan<KeyValuePair<string, object>>(keyValuePairs.ToArray());
    }

    internal static ReadOnlySpan<KeyValuePair<string, object>> AsReadonlySpan(this IEnumerable<KeyValuePair<string, object>> keyValuePairs)
    {
        return new ReadOnlySpan<KeyValuePair<string, object>>(keyValuePairs.ToArray());
    }
}
