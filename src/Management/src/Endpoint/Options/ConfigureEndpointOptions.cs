// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Options;

public class ConfigureEndpointOptions<T> : IConfigureOptions<T>
    where T : EndpointOptionsBase
{
    private readonly string _prefix;
    private readonly string _id;
    protected readonly IConfiguration _configuration;

    public ConfigureEndpointOptions(IConfiguration configuration, string prefix, string id)
    {
        _configuration = configuration;
        _prefix = prefix;
        _id = id;
    }

    public virtual void Configure(T options)
    {
        _configuration.GetSection(_prefix).Bind(options);

        if (options.Id == null)
        {
            options.Id = _id;
        }

        if (options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }
    }
}
