// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Hypermedia;

/// <summary>
/// Actuator Endpoint provider the hypermedia link collection for all registered and enabled actuators.
/// </summary>
internal sealed class ActuatorEndpoint : IActuatorEndpoint
{
    private readonly ILogger<ActuatorEndpoint> _logger;
    private readonly IOptionsMonitor<HypermediaEndpointOptions> _options;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOption;
    private readonly IEnumerable<IEndpointOptions> _endpointOptions;

    public IEndpointOptions Options => _options.CurrentValue;

    public ActuatorEndpoint(IOptionsMonitor<HypermediaEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IEnumerable<IEndpointOptions> endpointOptions, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(managementOptions);
        ArgumentGuard.NotNull(endpointOptions);
        ArgumentGuard.NotNull(loggerFactory);
        _options = options;
        _managementOption = managementOptions;
        _endpointOptions = endpointOptions;
        _logger = loggerFactory.CreateLogger<ActuatorEndpoint>();
    }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        var service = new HypermediaService(_managementOption, _options, _endpointOptions, _logger);
        return Task.Run(() => service.Invoke(baseUrl), cancellationToken);
    }
}
