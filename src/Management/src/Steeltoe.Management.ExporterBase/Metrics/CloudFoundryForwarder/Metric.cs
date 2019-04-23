// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class Metric
    {
        public Metric(string name, MetricType type, long timestamp, string unit, IDictionary<string, string> tags, double value)
        {
            this.Name = name;
            this.Type = type.ToString().ToLowerInvariant();
            this.Timestamp = timestamp;
            this.Tags = tags;
            this.Unit = unit;
            this.Value = value;
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
