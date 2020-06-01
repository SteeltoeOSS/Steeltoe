// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonApplicationsRoot
    {
        [JsonProperty("applications")]
        public JsonApplications Applications { get; set; }

        internal static JsonApplicationsRoot Deserialize(Stream stream)
        {
            return JsonSerialization.Deserialize<JsonApplicationsRoot>(stream);
        }
    }
}
