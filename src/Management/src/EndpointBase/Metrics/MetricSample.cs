// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricSample
    {
        [JsonProperty("statistic")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MetricStatistic Statistic { get; }

        [JsonProperty("value")]
        public double Value { get; }

        [JsonIgnore]
        public IEnumerable<KeyValuePair<string, string>> Tags { get; set; }

        public MetricSample(MetricStatistic statistic, double value, IEnumerable<KeyValuePair<string, string>> tags = null)
        {
            Statistic = statistic;
            Value = value;
            Tags = tags;
        }

        public override string ToString()
        {
            return "MeasurementSample{" + "Statistic=" + Statistic.ToString() + ", Value="
                    + Value + '}';
        }
    }
}
