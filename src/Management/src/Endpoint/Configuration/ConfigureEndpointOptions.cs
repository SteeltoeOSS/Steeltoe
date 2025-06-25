// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Configuration;

public abstract class ConfigureEndpointOptions<TOptions> : IConfigureOptionsWithKey<TOptions>
    where TOptions : EndpointOptions
{
    private readonly string _id;
    private readonly IConfiguration _configuration;

    public string ConfigurationKey { get; }

    protected ConfigureEndpointOptions(IConfiguration configuration, string configurationKey, string id)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(configurationKey);
        ArgumentNullException.ThrowIfNull(id);

        _configuration = configuration;
        ConfigurationKey = configurationKey;
        _id = id;
    }

    public virtual void Configure(TOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _configuration.GetSection(ConfigurationKey).Bind(options);

        options.Id ??= _id;

        if (options.AllowedVerbs.Count == 0)
        {
            options.ApplyDefaultAllowedVerbs();
        }

        if (!Enum.IsDefined(options.RequiredPermissions))
        {
            options.RequiredPermissions = EndpointPermissions.Restricted;
        }
    }
}
