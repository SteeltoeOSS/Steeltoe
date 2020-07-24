// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonApplication
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("instance")]
        [JsonConverter(typeof(JsonInstanceInfoConverter))]
        public IList<JsonInstanceInfo> Instances { get; set; }
    }
}
