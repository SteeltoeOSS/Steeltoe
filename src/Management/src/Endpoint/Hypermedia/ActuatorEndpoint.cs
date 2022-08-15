// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Hypermedia;

/// <summary>
/// Actuator Endpoint provider the hypermedia link collection for all registered and enabled actuators.
/// </summary>
public class ActuatorEndpoint : AbstractEndpoint<Links, string>, IActuatorEndpoint
{
    private readonly ILogger<ActuatorEndpoint> _logger;
    private readonly ActuatorManagementOptions _managementOption;

    protected new IActuatorHypermediaOptions Options => options as IActuatorHypermediaOptions;

    public ActuatorEndpoint(IActuatorHypermediaOptions options, ActuatorManagementOptions managementOptions, ILogger<ActuatorEndpoint> logger = null)
        : base(options)
    {
        _managementOption = managementOptions;
        _logger = logger;
    }

    public override Links Invoke(string baseUrl)
    {
        var service = new HypermediaService(_managementOption, options, _logger);
        return service.Invoke(baseUrl);
    }
}
