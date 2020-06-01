// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    internal class Application
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("managementUrl")]
        public Uri ManagementUrl { get; set; }

        [JsonProperty("healthUrl")]
        public Uri HealthUrl { get; set; }

        [JsonProperty("serviceUrl")]
        public Uri ServiceUrl { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal class Metadata
#pragma warning restore SA1402 // File may only contain a single type
    {
        [JsonProperty("startup")]
        public DateTimeOffset Startup { get; set; }
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal class RegistrationResult
#pragma warning restore SA1402 // File may only contain a single type
    {
        public string Id { get; set; }
    }
}
