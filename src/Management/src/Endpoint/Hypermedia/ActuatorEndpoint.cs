// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Hypermedia;

/// <summary>
/// Actuator Endpoint provider the hypermedia link collection for all registered and enabled actuators.
/// </summary>
public class ActuatorEndpoint : IActuatorEndpoint
{
    private readonly ILogger<ActuatorEndpoint> _logger;
    private readonly IOptionsMonitor<HypermediaEndpointOptions> _options;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOption;
    private readonly IEnumerable<IEndpointOptions> _endpointOptions;

    public IEndpointOptions Options => _options.CurrentValue;

    public ActuatorEndpoint(IOptionsMonitor<HypermediaEndpointOptions> options,
        IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IEnumerable<IEndpointOptions> endpointOptions,
        ILogger<ActuatorEndpoint> logger = null)
    {
        _options = options;
        _managementOption = managementOptions;
        _endpointOptions = endpointOptions;
        _logger = logger;
    }

    public virtual Links Invoke(string baseUrl)
    {
        var service = new HypermediaService(_managementOption, _options, _endpointOptions, _logger);
        return service.Invoke(baseUrl);
    }
}