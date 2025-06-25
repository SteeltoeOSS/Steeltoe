// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.CloudFoundry;

/// <summary>
/// CloudFoundryEndpointHandler provides hypermedia: a page is added with links to all the endpoints that are enabled. When deployed to CloudFoundry this
/// EndpointHandler is used for apps manager integration when <see cref="CloudFoundrySecurityMiddleware" /> is added.
/// </summary>
internal sealed class CloudFoundryEndpointHandler : ICloudFoundryEndpointHandler
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<CloudFoundryEndpointOptions> _endpointOptionsMonitor;
    private readonly IEndpointOptionsMonitorProvider[] _optionsMonitorProviderArray;
    private readonly ILogger<HypermediaService> _hypermediaServiceLogger;

    public EndpointOptions Options => _endpointOptionsMonitor.CurrentValue;

    public CloudFoundryEndpointHandler(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> endpointOptionsMonitor, IEnumerable<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        IEndpointOptionsMonitorProvider[] optionsMonitorProviderArray = endpointOptionsMonitorProviders.ToArray();
        ArgumentGuard.ElementsNotNull(optionsMonitorProviderArray);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptionsMonitor = endpointOptionsMonitor;
        _optionsMonitorProviderArray = optionsMonitorProviderArray;
        _hypermediaServiceLogger = loggerFactory.CreateLogger<HypermediaService>();
    }

    public async Task<Links> InvokeAsync(string baseUrl, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var hypermediaService =
            new HypermediaService(_managementOptionsMonitor, _endpointOptionsMonitor, _optionsMonitorProviderArray, _hypermediaServiceLogger);

        Links result = hypermediaService.Invoke(new Uri(baseUrl));
        return await Task.FromResult(result);
    }
}
