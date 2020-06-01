// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricSample
    {
        [JsonProperty("statistic")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MetricStatistic Statistic { get; }

        [JsonProperty("value")]
        public double Value { get; }

        public MetricSample(MetricStatistic statistic, double value)
        {
            Statistic = statistic;
            Value = value;
        }

        public override string ToString()
        {
            return "MeasurementSample{" + "Statistic=" + Statistic.ToString() + ", Value="
                    + Value + '}';
        }
    }
}
