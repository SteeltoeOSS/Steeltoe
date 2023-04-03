// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Refresh;

public class RefreshEndpoint : IRefreshEndpoint
{
    private readonly IOptionsMonitor<RefreshEndpointOptions> _options;

    private readonly IConfiguration _configuration;

    public IEndpointOptions Options => _options.CurrentValue;

    public RefreshEndpoint(IOptionsMonitor<RefreshEndpointOptions> options, IConfiguration configuration, ILogger<RefreshEndpoint> logger = null)
    {
        ArgumentGuard.NotNull(configuration);
        _options = options;
        _configuration = configuration;
    }

    public IList<string> Invoke()
    {
        return DoInvoke(_configuration);
    }

    public IList<string> DoInvoke(IConfiguration configuration)
    {
        if (configuration is IConfigurationRoot root)
        {
            root.Reload();
        }

        if (!_options.CurrentValue.ReturnConfiguration)
        {
            return new List<string>();
        }

        var keys = new List<string>();

        foreach (KeyValuePair<string, string> kvp in configuration.AsEnumerable())
        {
            keys.Add(kvp.Key);
        }

        return keys;
    }
}
