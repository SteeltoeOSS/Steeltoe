// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class Instance
    {
        public Instance(string instanceId, string instanceIndex, IList<Metric> metrics)
        {
            Id = instanceId;
            Index = instanceIndex;
            Metrics = metrics;
        }

        [JsonProperty("id")]
        public string Id { get; }

        [JsonProperty("index")]
        public string Index { get; }

        [JsonProperty("metrics")]
        public IList<Metric> Metrics { get; }
    }
}
