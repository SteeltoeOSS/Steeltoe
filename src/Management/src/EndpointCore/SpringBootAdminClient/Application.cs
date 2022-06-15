// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

[Obsolete("This class is moving to EndpointBase")]
internal sealed class Application
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("managementUrl")]
    public Uri ManagementUrl { get; set; }

    [JsonPropertyName("healthUrl")]
    public Uri HealthUrl { get; set; }

    [JsonPropertyName("serviceUrl")]
    public Uri ServiceUrl { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; }
}

#pragma warning disable SA1402 // File may only contain a single type
[Obsolete("This class is moving to EndpointBase")]
internal sealed class RegistrationResult
#pragma warning restore SA1402 // File may only contain a single type
{
    public string Id { get; set; }
}
