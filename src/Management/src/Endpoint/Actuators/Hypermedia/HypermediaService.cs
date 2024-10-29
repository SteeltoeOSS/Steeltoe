// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Configuration;
using Steeltoe.Management.Endpoint.Actuators.CloudFoundry;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Hypermedia;

internal sealed class HypermediaService
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly EndpointOptions _endpointOptions;
    private readonly ICollection<IEndpointOptionsMonitorProvider> _endpointOptionsMonitorProviders;
    private readonly ILogger<HypermediaService> _logger;

    public HypermediaService(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HypermediaEndpointOptions> hypermediaEndpointOptionsMonitor,
        ICollection<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders, ILogger<HypermediaService> logger)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(hypermediaEndpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentGuard.ElementsNotNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptions = hypermediaEndpointOptionsMonitor.CurrentValue;
        _endpointOptionsMonitorProviders = endpointOptionsMonitorProviders;
        _logger = logger;
    }

    public HypermediaService(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> cloudFoundryEndpointOptionsMonitor,
        ICollection<IEndpointOptionsMonitorProvider> endpointOptionsMonitorProviders, ILogger<HypermediaService> logger)
    {
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(cloudFoundryEndpointOptionsMonitor);
        ArgumentNullException.ThrowIfNull(endpointOptionsMonitorProviders);
        ArgumentGuard.ElementsNotNull(endpointOptionsMonitorProviders);
        ArgumentNullException.ThrowIfNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptions = cloudFoundryEndpointOptionsMonitor.CurrentValue;
        _endpointOptionsMonitorProviders = endpointOptionsMonitorProviders;
        _logger = logger;
    }

    public Links Invoke(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var links = new Links();

        if (!_endpointOptions.IsEnabled(_managementOptionsMonitor.CurrentValue))
        {
            return links;
        }

        _logger.LogTrace("Processing hypermedia for {ManagementOptions}", _managementOptionsMonitor.CurrentValue);

        Link? selfLink = null;

        IEnumerable<EndpointOptions> enabledEndpointOptions = _endpointOptionsMonitorProviders.Select(provider => provider.Get())
                                                                    .Where(endpointOptions => endpointOptions.IsEnabled(_managementOptionsMonitor.CurrentValue));

        var baseUri = new Uri(baseUrl);
        bool skipExposureCheck = baseUri.PathAndQuery.StartsWith(ConfigureManagementOptions.DefaultCloudFoundryPath, StringComparison.Ordinal);

        IEnumerable<EndpointOptions> enabledAndExposedEndpointOptions =
            skipExposureCheck ? enabledEndpointOptions : enabledEndpointOptions.TakeWhile(endpointOptions => endpointOptions.IsExposed(_managementOptionsMonitor.CurrentValue));

        foreach (EndpointOptions endpointOptions in enabledAndExposedEndpointOptions)
        {
            if (endpointOptions.Id == _endpointOptions.Id)
            {
                selfLink = new Link(baseUrl);
            }
            else
            {
                if (string.IsNullOrEmpty(endpointOptions.Id))
                {
                    continue;
                }

                if (!links.Entries.ContainsKey(endpointOptions.Id))
                {
                    string linkPath = $"{baseUrl.TrimEnd('/')}/{endpointOptions.Path}";
                    links.Entries.Add(endpointOptions.Id, new Link(linkPath));
                }
                else
                {
                    _logger.LogWarning("Duplicate endpoint ID detected: {DuplicateEndpointId}", endpointOptions.Id);
                }
            }
        }

        if (selfLink != null)
        {
            links.Entries.Add("self", selfLink);
        }

        return links;
    }
}
