// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class LockInfo
    {
        [JsonProperty("className")]
        public string ClassName { get; set; }

        [JsonProperty("identityHashCode")]
        public int IdentityHashCode { get; set; }
    }
}
