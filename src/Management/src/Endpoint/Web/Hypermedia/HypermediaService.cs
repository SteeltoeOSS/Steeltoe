// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Web.Hypermedia;

internal sealed class HypermediaService
{
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly EndpointOptions _endpointOptions;
    private readonly ICollection<EndpointOptions> _endpointOptionsCollection;
    private readonly ILogger _logger;

    public HypermediaService(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HypermediaEndpointOptions> hypermediaEndpointOptionsMonitor, ICollection<EndpointOptions> endpointOptionsCollection, ILogger logger)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(hypermediaEndpointOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsCollection);
        ArgumentGuard.NotNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptions = hypermediaEndpointOptionsMonitor.CurrentValue;
        _endpointOptionsCollection = endpointOptionsCollection;
        _logger = logger;
    }

    public HypermediaService(IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<CloudFoundryEndpointOptions> cloudFoundryEndpointOptionsMonitor, ICollection<EndpointOptions> endpointOptionsCollection, ILogger logger)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(cloudFoundryEndpointOptionsMonitor);
        ArgumentGuard.NotNull(endpointOptionsCollection);
        ArgumentGuard.NotNull(logger);

        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptions = cloudFoundryEndpointOptionsMonitor.CurrentValue;
        _endpointOptionsCollection = endpointOptionsCollection;
        _logger = logger;
    }

    public Links Invoke(string baseUrl)
    {
        ArgumentGuard.NotNull(baseUrl);

        var links = new Links();

        if (!_endpointOptions.IsEnabled(_managementOptionsMonitor.CurrentValue))
        {
            return links;
        }

        _logger.LogTrace("Processing hypermedia for {ManagementOptions}", _managementOptionsMonitor.CurrentValue);

        Link selfLink = null;

        foreach (EndpointOptions endpointOptions in _endpointOptionsCollection)
        {
            if (!endpointOptions.IsEnabled(_managementOptionsMonitor.CurrentValue) || !endpointOptions.IsExposed(_managementOptionsMonitor.CurrentValue))
            {
                continue;
            }

            if (endpointOptions.Id == _endpointOptions.Id)
            {
                selfLink = new Link(baseUrl);
            }
            else
            {
                if (!string.IsNullOrEmpty(endpointOptions.Id))
                {
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
        }

        if (selfLink != null)
        {
            links.Entries.Add("self", selfLink);
        }

        return links;
    }
}
