// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonApplication
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("instance")]
        [JsonConverter(typeof(JsonInstanceInfoConverter))]
        public IList<JsonInstanceInfo> Instances { get; set; }

        [JsonConstructor]
        public JsonApplication()
        {
        }

        internal static JsonApplication Deserialize(Stream stream)
        {
            return JsonSerialization.Deserialize<JsonApplication>(stream);
        }
    }
}
