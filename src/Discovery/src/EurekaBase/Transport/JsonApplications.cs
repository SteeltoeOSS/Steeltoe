// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonApplications
    {
        [JsonProperty("apps__hashcode")]
        public string AppsHashCode { get; set; }

        [JsonProperty("versions__delta")]
        public long VersionDelta { get; set; }

        [JsonProperty("application")]
        [JsonConverter(typeof(JsonApplicationConverter))]
        public IList<JsonApplication> Applications { get; set; }

        public JsonApplications()
        {
        }

        internal static JsonApplications Deserialize(Stream stream)
        {
            return JsonSerialization.Deserialize<JsonApplications>(stream);
        }
    }
}
