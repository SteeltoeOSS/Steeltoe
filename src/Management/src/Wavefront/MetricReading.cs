// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Wavefront;
internal class MetricReading
{
    public MetricReading(string key, double value, long? timestamp, IDictionary<string, string?> tags)
    {
        Key = key;
        Value = value;
        Timestamp = timestamp;
        Tags = tags;
    }
    public MetricReading(string key, double value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; internal set; }
    public double Value { get; internal set; }
    public long? Timestamp { get; internal set; }
    public IDictionary<string, string?> Tags { get; internal set; } = new Dictionary<string, string?>();
}
