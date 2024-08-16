// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Metrics;

public sealed class MetricsRequest
{
    public string MetricName { get; }

    public IList<KeyValuePair<string, string>> Tags { get; }

    public MetricsRequest(string metricName, IList<KeyValuePair<string, string>> tags)
    {
        ArgumentException.ThrowIfNullOrEmpty(metricName);
        ArgumentNullException.ThrowIfNull(tags);

        MetricName = metricName;
        Tags = tags;
    }
}
