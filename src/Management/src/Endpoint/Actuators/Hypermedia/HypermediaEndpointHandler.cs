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
internal sealed class HypermediaEndpointHandler : IHypermediaEndpointHandler
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<HypermediaEndpointOptions> _endpointOptionsMonitor;
    private readonly IEndpointOptionsMonitorProvider[] _endpointOptionsMonitorProviderArray;
    private readonly ILogger<HypermediaService> _hypermediaServiceLogger;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public HypermediaEndpointHandler(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HypermediaEndpointOptions> endpointOptionsMonitor, IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IEndpointOptionsMonitorProvider[] endpointOptionsMonitorProviderArray = endpointOptionsMonitorProviders.ToArray();
        ArgumentGuard.ElementsNotNull(endpointOptionsMonitorProviderArray);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _endpointOptionsMonitorProviderArray = endpointOptionsMonitorProviderArray;
        _hypermediaServiceLogger = loggerFactory.CreateLogger<HypermediaService>();
    }

    public Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var service = new HypermediaService(_managementOptionsMonitor, _endpointOptionsMonitor, _endpointOptionsMonitorProviderArray, _hypermediaServiceLogger);
        Links result = service.Invoke(new Uri(baseUrl));
        return Task.FromResult(result);
    }
}
