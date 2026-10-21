// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.ServiceRegistry;

public sealed class ServiceRegistryEndpointOptions : EndpointOptions
{
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
