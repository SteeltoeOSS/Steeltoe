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
    private readonly IEnumerable<HttpMiddlewareOptions> _endpointOptions;
    private readonly ILogger _logger;
    private readonly IOptionsMonitor<ManagementEndpointOptions> _managementOptionsMonitor;
    private readonly HttpMiddlewareOptions _options;

    public HypermediaService(IOptionsMonitor<ManagementEndpointOptions> managementOptionsMonitor, IOptionsMonitor<HypermediaEndpointOptions> options,
        IEnumerable<HttpMiddlewareOptions> endpointOptions, ILogger logger)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(logger);

        _logger = logger;
        _managementOptionsMonitor = managementOptionsMonitor;
        _endpointOptions = endpointOptions;
        _options = options.CurrentValue;
    }

    public HypermediaService(IOptionsMonitor<ManagementEndpointOptions> managementOptionsMonitor, IOptionsMonitor<CloudFoundryEndpointOptions> options,
        IEnumerable<HttpMiddlewareOptions> endpointOptions, ILogger logger)
    {
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(logger);

        _endpointOptions = endpointOptions;
        _logger = logger;
        _options = options.CurrentValue;
        _managementOptionsMonitor = managementOptionsMonitor;
    }

    public Links Invoke(string baseUrl)
    {
        ArgumentGuard.NotNull(baseUrl);
        var links = new Links();

        if (!_options.IsEnabled(_managementOptionsMonitor.CurrentValue))
        {
            return links;
        }

        _logger.LogTrace("Processing hypermedia for {ManagementOptions}", _managementOptionsMonitor.CurrentValue);

        Link selfLink = null;

        foreach (HttpMiddlewareOptions opt in _endpointOptions)
        {
            if (!opt.IsEnabled(_managementOptionsMonitor.CurrentValue) || !opt.IsExposed(_managementOptionsMonitor.CurrentValue))
            {
                continue;
            }

            if (opt.Id == _options.Id)
            {
                selfLink = new Link(baseUrl);
            }
            else
            {
                if (!string.IsNullOrEmpty(opt.Id))
                {
                    if (!links.LinkCollection.ContainsKey(opt.Id))
                    {
                        string linkPath = $"{baseUrl.TrimEnd('/')}/{opt.Path}";
                        links.LinkCollection.Add(opt.Id, new Link(linkPath));
                    }
                    else if (links.LinkCollection.ContainsKey(opt.Id))
                    {
                        _logger.LogWarning("Duplicate endpoint ID detected: {DuplicateEndpointId}", opt.Id);
                    }
                }
            }
        }

        if (selfLink != null)
        {
            links.LinkCollection.Add("self", selfLink);
        }

        return links;
    }
}
