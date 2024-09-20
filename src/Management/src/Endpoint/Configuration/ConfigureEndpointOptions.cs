// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Configuration;

internal abstract class ConfigureEndpointOptions<T> : IConfigureOptionsWithKey<T>
    where T : EndpointOptions
{
    private readonly string _prefix;
    private readonly string _id;

    string IConfigureOptionsWithKey<T>.ConfigurationKey => _prefix;

    protected IConfiguration Configuration { get; }

    protected ConfigureEndpointOptions(IConfiguration configuration, string prefix, string id)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(prefix);
        ArgumentNullException.ThrowIfNull(id);

        Configuration = configuration;
        _prefix = prefix;
        _id = id;
    }

    public virtual void Configure(T options)
    {
        ConfigureAtKey(Configuration, _prefix, _id, options);
    }

    private static void ConfigureAtKey(IConfiguration configuration, string configurationKey, string endpointId, T options)
    {
        ArgumentNullException.ThrowIfNull(options);

        configuration.GetSection(configurationKey).Bind(options);

        options.Id ??= endpointId;

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
