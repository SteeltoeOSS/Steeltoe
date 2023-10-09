// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Options;
internal abstract class ConfigureEndpointOptions<T> : IConfigureOptionsWithKey<T>
    where T : EndpointOptions
{
    private readonly string _prefix;
    private readonly string _id;
    private readonly IConfiguration _configuration;
    
    protected IConfiguration Configuration => _configuration;

    string IConfigureOptionsWithKey<T>.ConfigurationKey => _prefix;

    protected ConfigureEndpointOptions(IConfiguration configuration, string prefix, string id)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(prefix);
        ArgumentGuard.NotNull(id);

        _configuration = configuration;
        _prefix = prefix;
        _id = id;
    }

    public virtual void Configure(T options)
    {
        ArgumentGuard.NotNull(options);

        Configuration.GetSection(_prefix).Bind(options);

        options.Id ??= _id;

        if (options.RequiredPermissions == Permissions.Undefined)
        {
            options.RequiredPermissions = Permissions.Restricted;
        }
    }
}
