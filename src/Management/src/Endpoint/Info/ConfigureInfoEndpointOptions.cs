// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Info;

public class ConfigureInfoEndpointOptions : IConfigureOptions<InfoEndpointOptions>
{
    private const string ManagementInfoPrefix = "management:endpoints:info";
    public ConfigureInfoEndpointOptions(IConfiguration configuration)
    {
        this.configuration = configuration;
    }

    private IConfiguration configuration;

    public void Configure(InfoEndpointOptions options)
    {
        configuration.GetSection(ManagementInfoPrefix).Bind(options);

        if (string.IsNullOrEmpty(options.Id))
        {
            options.Id = "info";
        }

        if (options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }
    }

}
