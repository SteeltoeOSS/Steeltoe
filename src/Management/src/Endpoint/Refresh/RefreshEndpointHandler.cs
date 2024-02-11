// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Environment;

namespace Steeltoe.Management.Endpoint.Refresh;

internal sealed class RefreshEndpointHandler : IRefreshEndpointHandler
{
    private readonly IOptionsMonitor<RefreshEndpointOptions> _optionsMonitor;
    private readonly IConfiguration _configuration;
    private readonly Sanitizer _sanitizer;
    private readonly ILogger<RefreshEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public RefreshEndpointHandler(IOptionsMonitor<RefreshEndpointOptions> optionsMonitor, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _configuration = configuration;
        _sanitizer = new Sanitizer(optionsMonitor.CurrentValue.KeysToSanitize);
        _logger = loggerFactory.CreateLogger<RefreshEndpointHandler>();
    }

    public Task<IList<string>> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Refreshing Configuration");

        if (_configuration is IConfigurationRoot root)
        {
            root.Reload();
        }

        IList<string> keys = new List<string>();

        if (_optionsMonitor.CurrentValue.ReturnConfiguration)
        {
            foreach (KeyValuePair<string, string?> kvp in _configuration.AsEnumerable())
            {
                // _sanitizer.Sanitize(kvp.Key, kvp.Value);

                keys.Add(kvp.Key);
            }
        }

        return Task.FromResult(keys);
    }
}
