// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Web.Hypermedia;

/// <summary>
/// Provides the hypermedia link collection for all registered and enabled actuators.
/// </summary>
internal sealed class ActuatorEndpointHandler : IActuatorEndpointHandler
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<HypermediaEndpointOptions> _endpointOptionsMonitor;
    private readonly ICollection<EndpointOptions> _endpointOptionsCollection;
    private readonly ILogger<ActuatorEndpointHandler> _logger;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public ActuatorEndpointHandler(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HypermediaEndpointOptions> endpointOptionsMonitor, IEnumerable<EndpointOptions> endpointOptionsCollection, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsCollection);
        ArgumentGuard.NotNull(loggerFactory);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _endpointOptionsCollection = endpointOptionsCollection.ToList();
        _logger = loggerFactory.CreateLogger<ActuatorEndpointHandler>();
    }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(baseUrl);

        var service = new HypermediaService(_managementOptionsMonitor, _endpointOptionsMonitor, _endpointOptionsCollection, _logger);
        Links result = service.Invoke(baseUrl);
        return Task.FromResult(result);
    }
}
