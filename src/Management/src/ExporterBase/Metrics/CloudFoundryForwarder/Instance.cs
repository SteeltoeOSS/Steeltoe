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
