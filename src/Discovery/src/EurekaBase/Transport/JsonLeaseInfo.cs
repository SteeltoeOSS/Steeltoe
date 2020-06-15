// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System.IO;

namespace Steeltoe.Discovery.Eureka.Transport
{
    internal class JsonLeaseInfo
    {
        [JsonProperty("renewalIntervalInSecs")]
        public int RenewalIntervalInSecs { get; set; }

        [JsonProperty("durationInSecs")]
        public int DurationInSecs { get; set; }

        [JsonProperty("registrationTimestamp")]
        public long RegistrationTimestamp { get; set; }

        [JsonProperty("lastRenewalTimestamp")]
        public long LastRenewalTimestamp { get; set; }

        [JsonProperty("renewalTimestamp")]
        public long LastRenewalTimestampLegacy { get; set; }

        [JsonProperty("evictionTimestamp")]
        public long EvictionTimestamp { get; set; }

        [JsonProperty("serviceUpTimestamp")]
        public long ServiceUpTimestamp { get; set; }

        internal static JsonLeaseInfo Deserialize(Stream stream)
        {
            return JsonSerialization.Deserialize<JsonLeaseInfo>(stream);
        }
    }
}
