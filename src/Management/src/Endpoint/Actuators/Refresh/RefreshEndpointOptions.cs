// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Refresh;

public sealed class RefreshEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to return the configuration after refreshing. Default value: true.
    /// </summary>
    public bool ReturnConfiguration { get; set; } = true;

    /// <inheritdoc />
    protected override IList<string> GetDefaultAllowedVerbs()
    {
        return new List<string>
        {
            "Post"
        };
    }
}
