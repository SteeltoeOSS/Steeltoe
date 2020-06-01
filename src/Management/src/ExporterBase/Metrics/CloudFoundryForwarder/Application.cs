// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public class Application
    {
        public Application(string applicationId, List<Instance> instances)
        {
            Id = applicationId;
            Instances = instances;
        }

        [JsonProperty("id")]
        public string Id { get; }

        [JsonProperty("instances")]
        public IList<Instance> Instances { get; }
    }
}
