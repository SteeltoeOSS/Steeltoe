// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Environment;

public sealed class EnvironmentResponse
{
    [JsonPropertyName("activeProfiles")]
    public IList<string> ActiveProfiles { get; }

    [JsonPropertyName("propertySources")]
    public IList<PropertySourceDescriptor> PropertySources { get; }

    public EnvironmentResponse(IList<string> activeProfiles, IList<PropertySourceDescriptor> sources)
    {
        ArgumentNullException.ThrowIfNull(activeProfiles);
        ArgumentGuard.ElementsNotNullOrWhiteSpace(activeProfiles);
        ArgumentNullException.ThrowIfNull(sources);

        ActiveProfiles = activeProfiles;
        PropertySources = sources;
    }
}
