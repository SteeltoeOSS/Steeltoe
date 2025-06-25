// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Refresh;

internal sealed class RefreshEndpointHandler : IRefreshEndpointHandler
{
    private readonly IOptionsMonitor<RefreshEndpointOptions> _optionsMonitor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RefreshEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public RefreshEndpointHandler(IOptionsMonitor<RefreshEndpointOptions> optionsMonitor, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<RefreshEndpointHandler>();
    }

    public Task<IList<string>> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing Configuration");

        if (_configuration is not IConfigurationRoot root)
        {
            throw new InvalidOperationException("The configuration is not a root configuration and cannot be reloaded.");
        }

        root.Reload();

        SortedSet<string> keys = new(StringComparer.OrdinalIgnoreCase);

        if (_optionsMonitor.CurrentValue.ReturnConfiguration)
        {
            foreach ((string key, _) in _configuration.AsEnumerable())
            {
                keys.Add(key);
            }
        }

        return Task.FromResult<IList<string>>(keys.ToList());
    }
}
