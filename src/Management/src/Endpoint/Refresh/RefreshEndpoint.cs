// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Refresh;

internal sealed class RefreshEndpoint : IRefreshEndpoint
{
    private readonly IOptionsMonitor<RefreshEndpointOptions> _options;

    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshEndpoint> _logger;

    public IHttpMiddlewareOptions Options => _options.CurrentValue;

    public RefreshEndpoint(IOptionsMonitor<RefreshEndpointOptions> options, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<RefreshEndpoint>();
    }

    public Task<IList<string>> InvokeAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => DoInvoke(_configuration), cancellationToken);
    }

    public IList<string> DoInvoke(IConfiguration configuration)
    {
        _logger.LogInformation("Refreshing Configuration");

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
