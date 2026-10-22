// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

public sealed class ServiceRegistryEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets the permissions required to access this endpoint, when running on Cloud Foundry. Default value: Full.
    /// </summary>
    public override EndpointPermissions RequiredPermissions { get; set; } = EndpointPermissions.Full;

    /// <inheritdoc />
    protected override IList<string> GetDefaultAllowedVerbs()
    {
        return new List<string>
        {
            "Get",
            "Post"
        };
    }
}
