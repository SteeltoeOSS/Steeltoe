using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    public class MetricTag
    {
        [JsonPropertyName("tag")]
        public string Tag { get; }

        [JsonPropertyName("values")]
        public ISet<string> Values { get; }

        public MetricTag(string tag, ISet<string> values)
        {
            Tag = tag;
            Values = values;
        }
    }
}
