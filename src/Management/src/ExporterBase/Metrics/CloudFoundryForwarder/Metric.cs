// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class Metric
    {
        public Metric(string name, MetricType type, long timestamp, string unit, IDictionary<string, string> tags, double value)
        {
            Name = name;
            Type = type.ToString().ToLowerInvariant();
            Timestamp = timestamp;
            Tags = tags;
            Unit = unit;
            Value = value;
        }

        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("tags")]
        public IDictionary<string, string> Tags { get; }

        [JsonProperty("timestamp")]
        public long Timestamp { get;  }

        [JsonProperty("type")]
        public string Type { get; }

        [JsonProperty("unit")]
        public string Unit { get; }

        [JsonProperty("value")]
        public double Value { get; }
    }
}
