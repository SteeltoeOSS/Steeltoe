// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class Application
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("managementUrl")]
    public Uri ManagementUrl { get; }

    [JsonPropertyName("healthUrl")]
    public Uri HealthUrl { get; }

    [JsonPropertyName("serviceUrl")]
    public Uri ServiceUrl { get; }

    [JsonPropertyName("metadata")]
    public IDictionary<string, object> Metadata { get; }

    public Application(string name, Uri managementUrl, Uri healthUrl, Uri serviceUrl, IDictionary<string, object> metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(managementUrl);
        ArgumentNullException.ThrowIfNull(healthUrl);
        ArgumentNullException.ThrowIfNull(serviceUrl);
        ArgumentNullException.ThrowIfNull(metadata);

        Name = name;
        ManagementUrl = managementUrl;
        HealthUrl = healthUrl;
        ServiceUrl = serviceUrl;
        Metadata = metadata;
    }
}
