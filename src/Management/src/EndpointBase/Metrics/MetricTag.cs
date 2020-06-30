// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.Metrics
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
