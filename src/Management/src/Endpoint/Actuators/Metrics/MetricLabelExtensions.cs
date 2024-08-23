// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

internal static class MetricLabelExtensions
{
    public static ReadOnlySpan<KeyValuePair<string, object?>> AsReadonlySpan(this IDictionary<string, object?> keyValuePairs)
    {
        ArgumentNullException.ThrowIfNull(keyValuePairs);

        return keyValuePairs.ToArray();
    }
}
