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
public class ActuatorEndpoint : /*AbstractEndpoint<Links, string>*/ IEndpoint<Links, string>, IActuatorEndpoint
{
    private readonly ILogger<ActuatorEndpoint> _logger;
    private IOptionsMonitor<HypermediaEndpointOptions> _options;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOption;

    //private readonly IActuatorHypermediaOptions _options;
    // private readonly ActuatorManagementOptions _managementOption;

    public IOptionsMonitor<HypermediaEndpointOptions> Options => _options;

    IEndpointOptions IEndpoint.Options => _options.CurrentValue;

    public ActuatorEndpoint(IOptionsMonitor<HypermediaEndpointOptions> options, IOptionsMonitor<ManagementEndpointOptions> managementOptions, ILogger<ActuatorEndpoint> logger = null)
        //: base(options)
    {
        _options = options;
        _managementOption = managementOptions;
        _logger = logger;
    }

    public Links Invoke(string baseUrl)
    {
        var service = new HypermediaService(_managementOption, _options, _logger);
        return service.Invoke(baseUrl);
    }
}
