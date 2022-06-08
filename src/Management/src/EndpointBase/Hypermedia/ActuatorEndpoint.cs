// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Hypermedia;

/// <summary>
/// Actuator Endpoint provider the hypermedia link collection for all registered and enabled actuators
/// </summary>
public class ActuatorEndpoint : AbstractEndpoint<Links, string>, IActuatorEndpoint
{
    private readonly ILogger<ActuatorEndpoint> _logger;
    private readonly ActuatorManagementOptions _mgmtOption;

    public ActuatorEndpoint(IActuatorHypermediaOptions options, ActuatorManagementOptions mgmtOptions, ILogger<ActuatorEndpoint> logger = null)
        : base(options)
    {
        _mgmtOption = mgmtOptions;
        _logger = logger;
    }

    protected new IActuatorHypermediaOptions Options => options as IActuatorHypermediaOptions;

    public override Links Invoke(string baseUrl)
    {
        var service = new HypermediaService(_mgmtOption, options, _logger);
        return service.Invoke(baseUrl);
    }
}
