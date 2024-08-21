// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Hypermedia;

/// <summary>
/// Provides the hypermedia link collection for all registered and enabled actuators.
/// </summary>
internal sealed class ActuatorEndpointHandler : IActuatorEndpointHandler
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<HypermediaEndpointOptions> _endpointOptionsMonitor;
    private readonly ICollection<EndpointOptions> _endpointOptionsCollection;
    private readonly ILogger<HypermediaService> _hypermediaServiceLogger;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public ActuatorEndpointHandler(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HypermediaEndpointOptions> endpointOptionsMonitor, IEnumerable<EndpointOptions> endpointOptionsCollection, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsCollection);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        EndpointOptions[] endpointOptionsArray = endpointOptionsCollection.ToArray();
        ArgumentGuard.ElementsNotNull(endpointOptionsArray);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _endpointOptionsCollection = endpointOptionsArray;
        _hypermediaServiceLogger = loggerFactory.CreateLogger<HypermediaService>();
    }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var service = new HypermediaService(_managementOptionsMonitor, _endpointOptionsMonitor, _endpointOptionsCollection, _hypermediaServiceLogger);
        Links result = service.Invoke(baseUrl);
        return Task.FromResult(result);
    }
}
