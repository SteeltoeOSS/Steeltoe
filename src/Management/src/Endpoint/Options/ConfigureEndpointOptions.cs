// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Options;

internal class ConfigureEndpointOptions<T> : IConfigureOptions<T>
    where T : class
{
    private readonly string _prefix;
    private readonly string _id;
    protected IConfiguration Configuration { get; }

    public ConfigureEndpointOptions(IConfiguration configuration, string prefix, string id)
    {
        Configuration = configuration;
        _prefix = prefix;
        _id = id;
    }

    public virtual void Configure(T options)
    {
        ArgumentGuard.NotNull(options);

        Configuration.GetSection(_prefix).Bind(options);

    }
}
