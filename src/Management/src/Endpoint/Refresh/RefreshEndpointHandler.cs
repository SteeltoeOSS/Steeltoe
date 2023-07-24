// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Refresh;

internal sealed class RefreshEndpointHandler : IRefreshEndpointHandler
{
    private readonly IOptionsMonitor<RefreshEndpointOptions> _options;

    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public RefreshEndpointHandler(IOptionsMonitor<RefreshEndpointOptions> options, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(loggerFactory);

        _options = options;
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<RefreshEndpointHandler>();
    }

    public Task<IList<string>> InvokeAsync(object argument, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing Configuration");

        if (_configuration is IConfigurationRoot root)
        {
            root.Reload();
        }

        IList<string> keys = new List<string>();

        if (_options.CurrentValue.ReturnConfiguration)
        {
            foreach (KeyValuePair<string, string> kvp in _configuration.AsEnumerable())
            {
                keys.Add(kvp.Key);
            }
        }

        return Task.FromResult(keys);
    }
}
